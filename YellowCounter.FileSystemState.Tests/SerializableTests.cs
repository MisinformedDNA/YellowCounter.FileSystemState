using System.IO;
using Xunit;
using YellowCounter.FileSystemState;

public class FileSystemStateSerializableTests
{
    //[Fact]
    //public void RoundTripDoesNotAffectOriginalTest()
    //{
    //    string currentDir = Utility.GetRandomDirectory();
    //    string fileName = Path.GetRandomFileName() + ".txt";
    //    string fullName = Path.Combine(currentDir, fileName);

    //    FileSystemState state = new FileSystemState(currentDir, "*.csv");
    //    FileSystemState state2 = new FileSystemState(currentDir, "*.txt");

    //    state.LoadState();
    //    RoundTrip(state, state2);

    //    using (var file = File.Create(fullName)) { }

    //    try
    //    {
    //        Assert.Empty(state.GetChanges());
    //        Assert.Single(state2.GetChanges());
    //    }
    //    finally
    //    {
    //        Directory.Delete(currentDir, true);
    //    }
    //}

    //[Fact]
    //public void RoundTripVersionReset_NoChanges_Test()
    //{
    //    string currentDir = Utility.GetRandomDirectory();
    //    string fileName = Path.GetRandomFileName();
    //    string fullName = Path.Combine(currentDir, fileName);
    //    using (var file = File.Create(fullName)) { }

    //    FileSystemState state = new FileSystemState(currentDir);
    //    state.LoadState();
    //    state.GetChanges();

    //    FileSystemState state2 = new FileSystemState(currentDir);
    //    RoundTrip(state, state2);

    //    try
    //    {
    //        Assert.Empty(state.GetChanges());
    //        Assert.Empty(state2.GetChanges());
    //    }
    //    finally
    //    {
    //        Directory.Delete(currentDir, true);
    //    }
    //}

    //[Fact]
    //public void RoundTripVersionReset_Deletion_Test()
    //{
    //    string currentDir = Utility.GetRandomDirectory();
    //    string fileName = Path.GetRandomFileName();
    //    string fullName = Path.Combine(currentDir, fileName);
    //    using (var file = File.Create(fullName)) { }

    //    FileSystemState state = new FileSystemState(currentDir);
    //    state.LoadState();

    //    FileSystemState state2 = new FileSystemState(currentDir);
    //    RoundTrip(state, state2);
    //    File.Delete(fullName);

    //    try
    //    {
    //        Assert.Single(state.GetChanges());
    //        Assert.Single(state2.GetChanges());
    //    }
    //    finally
    //    {
    //        Directory.Delete(currentDir, true);
    //    }
    //}

    private static void RoundTrip(FileSystemState source, FileSystemState destination)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            source.SaveState(stream);

            stream.Position = 0;
            destination.LoadState(stream);
        }
    }
}
