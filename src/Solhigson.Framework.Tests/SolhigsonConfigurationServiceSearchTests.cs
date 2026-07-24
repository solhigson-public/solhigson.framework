using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Solhigson.Framework.Persistence;
using Solhigson.Framework.Persistence.EntityModels;
using Solhigson.Framework.Persistence.Repositories;
using Solhigson.Framework.Services;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Pins the case-INsensitive substring contract of the two admin config searches
/// (<see cref="SolhigsonConfigurationService.SearchAppSettingsAsync"/> and
/// <see cref="SolhigsonConfigurationService.SearchNotificationTemplatesAsync"/>) over a real SQLite in-memory DB
/// (per test-pattern rule; NEVER the EF InMemory provider). The predicate is <c>t.Name.ToLower().Contains(term.ToLower())</c>,
/// which translates to <c>instr(lower("Name"), @term)</c> on SQLite: <c>instr</c> is a BINARY (case-sensitive) match,
/// so a lowercase term against a mixed-case stored name matches ONLY because BOTH sides are lowered — the same
/// assertion FAILS under the pre-fix bare <c>t.Name.Contains(term)</c>. This is the provider-portable form (the
/// framework targets Sqlite + SqlServer, no Npgsql, so <c>EF.Functions.ILike</c> would not translate here). A shared
/// open connection lets a seed context and a query context see one DB, exactly like the sibling audit-capture tests.
/// </summary>
public sealed class SolhigsonConfigurationServiceSearchTests : IDisposable
{
    // The literal masked marker MaskForDisplayIfSensitive stamps onto a sensitive setting's Value on display
    // (mirrors the private SolhigsonConfigurationService.EncryptDisplay constant). Asserting the search returns
    // THIS (never the seeded plaintext) proves masking survives the predicate change.
    private const string MaskedDisplayMarker = "@@@***Encrypted***@@@";

    private readonly SqliteConnection _connection;

    public SolhigsonConfigurationServiceSearchTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
    }

    // ── SearchAppSettingsAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task SearchAppSettingsAsync_WithALowercasePartialTerm_MatchesAMixedCaseName()
    {
        using (var seed = CreateContext())
        {
            seed.Add(new AppSetting { Name = "StylesAndScriptsVersion", Value = "17", IsSensitive = false });
            seed.Add(new AppSetting { Name = "PaymentProviderTimeout", Value = "30", IsSensitive = false });
            await seed.SaveChangesAsync();
        }

        using var ctx = CreateContext();
        var service = new SolhigsonConfigurationService(new RepositoryWrapper(ctx));

        var response = await service.SearchAppSettingsAsync(name: "styles");

        response.IsSuccessful.ShouldBeTrue();
        response.Data.Results.Count.ShouldBe(1); // ONLY StylesAndScriptsVersion — the term is a substring match
        response.Data.Results.Single().Name.ShouldBe("StylesAndScriptsVersion");
    }

    [Fact]
    public async Task SearchAppSettingsAsync_WithAnUppercaseTerm_MatchesALowercaseStoredName()
    {
        // Proves the TERM is lowered too (not only the column): an uppercase query hits a lowercase stored name.
        using (var seed = CreateContext())
        {
            seed.Add(new AppSetting { Name = "paystack_secret", Value = "sk", IsSensitive = false });
            await seed.SaveChangesAsync();
        }

        using var ctx = CreateContext();
        var service = new SolhigsonConfigurationService(new RepositoryWrapper(ctx));

        var response = await service.SearchAppSettingsAsync(name: "PAYSTACK");

        response.IsSuccessful.ShouldBeTrue();
        response.Data.Results.Single().Name.ShouldBe("paystack_secret");
    }

    [Fact]
    public async Task SearchAppSettingsAsync_WithATermPresentInNoName_ReturnsAnEmptyPage()
    {
        using (var seed = CreateContext())
        {
            seed.Add(new AppSetting { Name = "StylesAndScriptsVersion", Value = "17", IsSensitive = false });
            await seed.SaveChangesAsync();
        }

        using var ctx = CreateContext();
        var service = new SolhigsonConfigurationService(new RepositoryWrapper(ctx));

        var response = await service.SearchAppSettingsAsync(name: "nonexistent");

        response.IsSuccessful.ShouldBeTrue();
        response.Data.Results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchAppSettingsAsync_MatchingASensitiveSetting_PreservesMaskingAndNeverUnmasks()
    {
        const string sensitivePlaintext = "super-secret-token-value";
        using (var seed = CreateContext())
        {
            seed.Add(new AppSetting { Name = "PaystackSecretKey", Value = sensitivePlaintext, IsSensitive = true });
            await seed.SaveChangesAsync();
        }

        using var ctx = CreateContext();
        var service = new SolhigsonConfigurationService(new RepositoryWrapper(ctx));

        var response = await service.SearchAppSettingsAsync(name: "secret");

        response.IsSuccessful.ShouldBeTrue();
        var match = response.Data.Results.Single();
        match.Name.ShouldBe("PaystackSecretKey");  // found by the case-insensitive predicate…
        match.Value.ShouldNotBe(sensitivePlaintext); // …yet its sensitive value is NOT unmasked…
        match.Value.ShouldBe(MaskedDisplayMarker);   // …the MaskForDisplayIfSensitive post-processing still runs
    }

    // ── SearchNotificationTemplatesAsync ────────────────────────────────────────

    [Fact]
    public async Task SearchNotificationTemplatesAsync_WithALowercasePartialTerm_MatchesAMixedCaseName()
    {
        using (var seed = CreateContext())
        {
            seed.Add(new NotificationTemplate { Name = "WelcomeEmailTemplate", Template = "<p>Hi</p>" });
            seed.Add(new NotificationTemplate { Name = "PasswordResetTemplate", Template = "<p>Reset</p>" });
            await seed.SaveChangesAsync();
        }

        using var ctx = CreateContext();
        var service = new SolhigsonConfigurationService(new RepositoryWrapper(ctx));

        var response = await service.SearchNotificationTemplatesAsync(name: "welcome");

        response.IsSuccessful.ShouldBeTrue();
        response.Data.Results.Count.ShouldBe(1); // ONLY WelcomeEmailTemplate
        response.Data.Results.Single().Name.ShouldBe("WelcomeEmailTemplate");
    }

    [Fact]
    public async Task SearchNotificationTemplatesAsync_WithATermPresentInNoName_ReturnsAnEmptyPage()
    {
        using (var seed = CreateContext())
        {
            seed.Add(new NotificationTemplate { Name = "WelcomeEmailTemplate", Template = "<p>Hi</p>" });
            await seed.SaveChangesAsync();
        }

        using var ctx = CreateContext();
        var service = new SolhigsonConfigurationService(new RepositoryWrapper(ctx));

        var response = await service.SearchNotificationTemplatesAsync(name: "nonexistent");

        response.IsSuccessful.ShouldBeTrue();
        response.Data.Results.ShouldBeEmpty();
    }

    // ── infrastructure ──────────────────────────────────────────────────────────

    private SolhigsonDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SolhigsonDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new SolhigsonDbContext(options);
    }

    public void Dispose() => _connection.Dispose();
}
