using System;
using System.IO;
using Shouldly;
using Solhigson.Framework.EfCoreTool.Generator;
using Xunit;

namespace Solhigson.Framework.Tests;

/// <summary>
/// Unit contract for CommandBase.SaveFile: a generated file is only written when its content actually
/// changed. The generated payload carries no timestamp, so equal text means equal file, and a rewrite
/// would only churn modification times (and therefore downstream build inputs) for no gain.
/// The comparison normalises CRLF to LF on BOTH sides: the payload's endings follow the checkout that
/// built the package, the on-disk copy's endings follow the consuming repository's core.autocrlf, and
/// normalising both is what makes the compare independent of either.
/// </summary>
public class SaveFileWriteOnChangeTests
{
    private static readonly DateTime FixedPastStamp = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private const string LfContent = "line one\nline two\nline three\n";

    [Fact]
    public void FirstWrite_CreatesTheFile()
    {
        using var dir = new TempDirectory();
        var path = dir.Path("Sample.generated.cs");

        File.Exists(path).ShouldBeFalse();

        CommandBase.SaveFile(LfContent, path);

        File.Exists(path).ShouldBeTrue();
        File.ReadAllText(path).ShouldBe(LfContent);
    }

    [Fact]
    public void SecondWrite_OfIdenticalContent_LeavesTimestampAndBytesUntouched()
    {
        using var dir = new TempDirectory();
        var path = dir.Path("Sample.generated.cs");

        CommandBase.SaveFile(LfContent, path);
        var bytesBefore = File.ReadAllBytes(path);
        File.SetLastWriteTimeUtc(path, FixedPastStamp);

        CommandBase.SaveFile(LfContent, path);

        File.GetLastWriteTimeUtc(path).ShouldBe(FixedPastStamp);
        File.ReadAllBytes(path).ShouldBe(bytesBefore);
    }

    [Fact]
    public void ChangedContent_IsWritten_AndShorterContentLeavesNoTailOfTheOldFile()
    {
        using var dir = new TempDirectory();
        var path = dir.Path("Sample.generated.cs");

        const string longContent = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\nbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\ncccccccccccccccccccccccccccccc\n";
        const string shortContent = "aaa\n";

        CommandBase.SaveFile(longContent, path);
        File.ReadAllText(path).ShouldBe(longContent);
        File.SetLastWriteTimeUtc(path, FixedPastStamp);

        CommandBase.SaveFile(shortContent, path);

        // Exact equality, not StartsWith: the old delete-then-OpenWrite shape would have left the
        // tail of the longer content behind if the delete were ever dropped.
        File.ReadAllText(path).ShouldBe(shortContent);
        new FileInfo(path).Length.ShouldBe((long)shortContent.Length);
        File.GetLastWriteTimeUtc(path).ShouldBeGreaterThan(FixedPastStamp);
    }

    [Fact]
    public void CrLfCopyOfLfContent_IsLeftUntouched()
    {
        using var dir = new TempDirectory();
        var path = dir.Path("Sample.generated.cs");

        CommandBase.SaveFile(LfContent, path);

        // Simulate a checkout under core.autocrlf=true: same text, CRLF on disk.
        File.WriteAllText(path, LfContent.Replace("\n", "\r\n"));
        var crLfBytes = File.ReadAllBytes(path);
        File.SetLastWriteTimeUtc(path, FixedPastStamp);

        CommandBase.SaveFile(LfContent, path);

        File.GetLastWriteTimeUtc(path).ShouldBe(FixedPastStamp);
        File.ReadAllBytes(path).ShouldBe(crLfBytes);
    }

    [Fact]
    public void ExistingNonGeneratedFile_IsStillSkipped()
    {
        using var dir = new TempDirectory();
        var path = dir.Path("Custom.cs");

        const string handWritten = "// hand written, never overwritten\n";
        File.WriteAllText(path, handWritten);
        File.SetLastWriteTimeUtc(path, FixedPastStamp);

        CommandBase.SaveFile("// tool output that must not land\n", path);

        File.ReadAllText(path).ShouldBe(handWritten);
        File.GetLastWriteTimeUtc(path).ShouldBe(FixedPastStamp);
    }

    private sealed class TempDirectory : IDisposable
    {
        private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
            System.IO.Path.GetRandomFileName());

        internal TempDirectory() => Directory.CreateDirectory(_root);

        internal string Path(string fileName) => System.IO.Path.Combine(_root, fileName);

        public void Dispose()
        {
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // A leaked temp directory must never fail a test run, read-only leftovers included.
            }
        }
    }
}
