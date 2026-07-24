using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Solhigson.Framework.Infrastructure;
using Solhigson.Framework.Persistence;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Pins the "Solhigson:SynchronousDefaultInsert" opt-in contract on <see cref="ConfigurationWrapper"/> over a
/// real SQLite in-memory DB (per test-pattern rule; NEVER the EF InMemory provider). The wrapper's default-insert
/// path fires ONLY when constructed with a non-null <see cref="DbContextOptionsBuilder{SolhigsonDbContext}"/>
/// (the null-optionsBuilder path returns early before any insert), so both tests bind the wrapper to an
/// optionsBuilder over a single shared open connection, exactly like the sibling search tests.
///
/// Contract asserted:
///  * FLAG ON  -> GetConfig AWAITS the auto-default insert, so the SolhigsonAppSettings row is present the
///    instant the awaited call returns (deterministic: no timing window).
///  * FLAG OFF (absent) -> GetConfig does NOT await the insert (fire-and-forget preserved), yet the insert is
///    still dispatched to the DB-write seam. Proven deterministically WITHOUT any Task.Delay/Sleep: a blocking
///    <see cref="ISaveChangesInterceptor"/> holds the background insert at its SaveChanges seam; the fact that
///    the awaited GetConfig call RETURNS (bounded by a WhenAny timeout) while that seam is still gated is itself
///    the proof of non-awaiting, and the interceptor's "reached" signal proves the insert was not dropped.
/// </summary>
public sealed class ConfigurationWrapperSynchronousInsertTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ConfigurationWrapperSynchronousInsertTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetConfig_WithSynchronousDefaultInsertOn_PersistsTheDefaultRowBeforeReturning()
    {
        // Setup: flag ON, an optionsBuilder over the shared connection, and a config key ABSENT from both
        // IConfiguration and the SolhigsonAppSettings table so the default-insert branch is reached.
        const string absentKey = "SynchronousDefaultInsertProbe";
        const string suppliedDefault = "42";
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Solhigson:SynchronousDefaultInsert"] = "true"
        });
        var wrapper = new ConfigurationWrapper(configuration, NewOptionsBuilder());

        // Exercise: the awaited GetConfig must both return the supplied default and — because the flag is ON —
        // synchronously commit the auto-default row before it returns.
        var value = await wrapper.GetConfigAsync<string>(absentKey, key: null, defaultValue: suppliedDefault);

        // Verify: value-correctness, then the row is present IMMEDIATELY on a fresh context over the same
        // connection (deterministic — the insert was awaited, so there is no race).
        value.ShouldBe(suppliedDefault);
        using var verify = CreateContext();
        var row = verify.AppSettings.SingleOrDefault(s => s.Name == absentKey);
        row.ShouldNotBeNull();
        row.Value.ShouldBe(suppliedDefault);
    }

    [Fact]
    public async Task GetConfig_WithSynchronousDefaultInsertOff_ReturnsWithoutAwaitingButStillDispatchesTheInsert()
    {
        // Setup: flag ABSENT (default OFF — also exercises the null-safe bool.TryParse(null) construction path).
        // A blocking interceptor gates the background insert at its SaveChanges seam so non-awaiting is provable
        // deterministically rather than by timing.
        const string absentKey = "FireAndForgetInsertProbe";
        const string suppliedDefault = "off-default";
        var configuration = BuildConfiguration(new Dictionary<string, string?>());
        var gate = new BlockingSaveChangesInterceptor();
        var wrapper = new ConfigurationWrapper(configuration, NewOptionsBuilder(gate));

        // Exercise: dispatch GetConfig. Under fire-and-forget it MUST return even though the (blocked) insert
        // cannot complete. Had the wrapper awaited the insert (the flag-ON behavior), this call would deadlock
        // on the still-closed gate — so the bounded completion below is the deterministic proof of non-awaiting.
        var getConfigTask = wrapper.GetConfigAsync<string>(absentKey, key: null, defaultValue: suppliedDefault);
        var settled = await Task.WhenAny(getConfigTask, Task.Delay(TimeSpan.FromSeconds(5)));

        // Verify (1): GetConfig returned while the insert seam is still gated -> it did not await the insert.
        settled.ShouldBe(getConfigTask, "flag-OFF GetConfig must return without awaiting the blocked default-insert");
        (await getConfigTask).ShouldBe(suppliedDefault);

        // Verify (2): the fire-and-forget insert was still dispatched and reached the DB-write seam (not dropped).
        var reached = await Task.WhenAny(gate.ReachedSaveChanges, Task.Delay(TimeSpan.FromSeconds(5)));
        reached.ShouldBe(gate.ReachedSaveChanges, "the fire-and-forget default-insert should still reach SaveChanges");

        // Teardown: release the gate so the background insert can finish; await it so nothing outlives the test.
        gate.Release();
        await gate.CompletedSaveChanges.WaitAsync(TimeSpan.FromSeconds(5));
    }

    // ── infrastructure ──────────────────────────────────────────────────────────

    private SolhigsonDbContext CreateContext()
        => new(new DbContextOptionsBuilder<SolhigsonDbContext>().UseSqlite(_connection).Options);

    private DbContextOptionsBuilder<SolhigsonDbContext> NewOptionsBuilder(ISaveChangesInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<SolhigsonDbContext>().UseSqlite(_connection);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }
        return builder;
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Test seam: blocks the FIRST SaveChanges at its interceptor boundary until <see cref="Release"/> is called,
    /// signalling <see cref="ReachedSaveChanges"/> when the boundary is hit and <see cref="CompletedSaveChanges"/>
    /// once the save proceeds. No Thread.Sleep / Task.Delay — pure TaskCompletionSource handshake.
    /// </summary>
    private sealed class BlockingSaveChangesInterceptor : ISaveChangesInterceptor
    {
        private readonly TaskCompletionSource _reached =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ReachedSaveChanges => _reached.Task;
        public Task CompletedSaveChanges => _completed.Task;

        public void Release() => _release.TrySetResult();

        public async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            _reached.TrySetResult();
            await _release.Task;
            return result;
        }

        public ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            _completed.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
}
