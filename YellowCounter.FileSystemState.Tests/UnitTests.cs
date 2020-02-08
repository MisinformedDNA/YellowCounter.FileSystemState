using System;
using System.IO;
using Xunit;
using YellowCounter.FileSystemState;

public partial class FileSystemStateUnitTests
{
    [Fact]
    public static void FileSystemWatcher_ctor_Defaults()
    {

        string path = Environment.CurrentDirectory;
        var watcher = new FileSystemState(path);
        Assert.Equal(path, watcher.Path);
        Assert.Equal("*", watcher.Filter);
        Assert.NotNull(watcher.EnumerationOptions);
    }

    [Fact]
    public static void FileSystemWatcher_ctor_OptionalParams()
    {
        string currentDir = Directory.GetCurrentDirectory();
        const string filter = "*.csv";
        var watcher = new FileSystemState(currentDir, filter, new EnumerationOptions { RecurseSubdirectories = true });

        Assert.Equal(currentDir, watcher.Path);
        Assert.Equal(filter, watcher.Filter);
        Assert.True(watcher.EnumerationOptions.RecurseSubdirectories);
    }

    [Fact]
    public static void FileSystemWatcher_ctor_Null()
    {
        // Not valid
        Assert.Throws<ArgumentNullException>("path", () => new FileSystemState(null));
        Assert.Throws<ArgumentNullException>("filter", () => new FileSystemState(Environment.CurrentDirectory, null));

        // Valid
        var watcher = new FileSystemState(Environment.CurrentDirectory, options: null);
    }

    [Fact]
    public static void FileSystemWatcher_ctor_PathDoesNotExist()
    {
        Assert.Throws<DirectoryNotFoundException>(() => new FileSystemState(@"Z:\RandomPath\sdsdljdkkjdfsdlcjfskdcvnj"));
    }


    [Fact]
    public static void FileSystemWatcher_Created_File()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName();
        string fullName = Path.Combine(currentDir, fileName);

        FileSystemState watcher = new FileSystemState(currentDir);
        watcher.LoadState();
        using (FileStream file = File.Create(fullName)) { }
        var changes = watcher.GetChanges();

        try
        {
            Assert.Single(changes);
            FileChange change = changes[0];
            Assert.Equal(WatcherChangeTypes.Created, change.ChangeType);
            Assert.Equal(fileName, change.Name);
            Assert.Equal(currentDir, change.Directory);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void FileSystemWatcher_Deleted_File()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName();
        string fullName = Path.Combine(currentDir, fileName);

        FileSystemState watcher = new FileSystemState(currentDir);
        using (FileStream file = File.Create(fullName)) { }
        watcher.LoadState();
        File.Delete(fullName);
        var changes = watcher.GetChanges();

        try
        {
            Assert.Single(changes);
            FileChange change = changes[0];
            Assert.Equal(WatcherChangeTypes.Deleted, change.ChangeType);
            Assert.Equal(fileName, change.Name);
            Assert.Equal(currentDir, change.Directory);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void FileSystemWatcher_Changed_File()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName();
        string fullName = Path.Combine(currentDir, fileName);

        FileSystemState watcher = new FileSystemState(currentDir);
        using (FileStream file = File.Create(fullName)) { }
        watcher.LoadState();
        File.AppendAllText(fullName, ".");
        var changes = watcher.GetChanges();

        try
        {
            Assert.Single(changes);
            FileChange change = changes[0];
            Assert.Equal(WatcherChangeTypes.Changed, change.ChangeType);
            Assert.Equal(fileName, change.Name);
            Assert.Equal(currentDir, change.Directory);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }



    [Fact]
    public static void FileSystemWatcher_Renamed_File()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName();
        string newName = Path.GetRandomFileName();
        string fullName = Path.Combine(currentDir, fileName);


        FileSystemState watcher = new FileSystemState(currentDir);

        using(FileStream file = File.Create(fullName)) { }
        watcher.LoadState();

        File.Move(fullName, Path.Combine(currentDir, newName));

        var changes = watcher.GetChanges();

        try
        {
            Assert.Single(changes);
            FileChange change = changes[0];
            Assert.Equal(WatcherChangeTypes.Renamed, change.ChangeType);
            Assert.Equal(fileName, change.OldName);
            Assert.Equal(currentDir, change.OldDirectory);
            Assert.Equal(newName, change.Name);
            Assert.Equal(currentDir, change.Directory);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void FileSystemWatcher_Filter()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = $"{Path.GetRandomFileName()}.csv";
        string fullName = Path.Combine(currentDir, fileName);

        FileSystemState watcher = new FileSystemState(currentDir, filter: "*.csv");
        watcher.LoadState();
        using (FileStream file = File.Create(fullName)) { }
        var changes = watcher.GetChanges();

        try
        {
            Assert.Single(changes);
            FileChange change = changes[0];
            Assert.Equal(WatcherChangeTypes.Created, change.ChangeType);
            Assert.Equal(fileName, change.Name);
            Assert.Equal(currentDir, change.Directory);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void FileSystemWatcher_Filter_Ignore()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = $"{Path.GetRandomFileName()}.txt";
        string fullName = Path.Combine(currentDir, fileName);

        FileSystemState watcher = new FileSystemState(currentDir, filter: "*.csv");
        watcher.LoadState();
        using (FileStream file = File.Create(fullName)) { }
        var changes = watcher.GetChanges();

        try
        {
            Assert.Empty(changes);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void FileSystemWatcher_NotRecursive()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName();
        string subDirectory = new DirectoryInfo(currentDir).CreateSubdirectory("sub").FullName;
        string fullName = Path.Combine(subDirectory, fileName);

        FileSystemState watcher = new FileSystemState(currentDir, options: new EnumerationOptions { RecurseSubdirectories = false });
        watcher.LoadState();
        using (FileStream file = File.Create(fullName)) { }
        var changes = watcher.GetChanges();

        try
        {
            Assert.Empty(changes);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    [Fact]
    public static void FileSystemWatcher_Recursive()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName();
        string subDirectory = new DirectoryInfo(currentDir).CreateSubdirectory("sub").FullName;
        string fullName = Path.Combine(subDirectory, fileName);

        FileSystemState watcher = new FileSystemState(currentDir, options: new EnumerationOptions { RecurseSubdirectories = true });
        watcher.LoadState();
        using (FileStream file = File.Create(fullName)) { }
        var changes = watcher.GetChanges();

        try
        {
            Assert.Single(changes);
            FileChange change = changes[0];
            Assert.Equal(WatcherChangeTypes.Created, change.ChangeType);
            Assert.Equal(fileName, change.Name);
            Assert.Equal(subDirectory, change.Directory);
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }
}
