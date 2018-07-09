using System.IO;
using System.IO.Enumeration;
using Xunit;
using YellowCounter.FileSystemState;

public partial class FileSystemStateDerivedTests
{
    [Trait("Category", "DerivedTests")]
    [Fact]
    public static void ShouldIncludeEntryTest()
    {
        string currentDir = Utility.GetRandomDirectory();
        string subDirectory = new DirectoryInfo(currentDir).CreateSubdirectory("sub").FullName;

        DerivedWatcher watcher2 = new DerivedWatcher(currentDir);
        watcher2.LoadState();

        try
        {
            Assert.True(watcher2.ShouldIncludeEntryCalled);
            Assert.False(watcher2.ShouldRecurseIntoEntryCalled);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void ShouldRecurseIntoEntryTest()
    {
        string currentDir = Utility.GetRandomDirectory();
        string subDirectory = new DirectoryInfo(currentDir).CreateSubdirectory("sub").FullName;

        DerivedWatcher watcher2 = new DerivedWatcher(currentDir, options: new EnumerationOptions { RecurseSubdirectories = true });
        watcher2.LoadState();

        try
        {
            Assert.True(watcher2.ShouldIncludeEntryCalled);
            Assert.True(watcher2.ShouldRecurseIntoEntryCalled);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }
}

public class DerivedWatcher : FileSystemState
{
    public DerivedWatcher(string path, string filter = "*.*", EnumerationOptions options = null) : base(path, filter, options)
    {
    }

    public bool ShouldIncludeEntryOverride { get; set; }
    public bool ShouldRecurseIntoEntryOverride { get; set; }

    public bool ShouldIncludeEntryCalled { get; set; }
    public bool ShouldRecurseIntoEntryCalled { get; set; }

    protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
    {
        ShouldIncludeEntryCalled = true;

        return base.ShouldIncludeEntry(ref entry);
    }

    protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
    {
        ShouldRecurseIntoEntryCalled = true;

        return base.ShouldRecurseIntoEntry(ref entry);
    }
}
