using System.IO;
using Xunit;
using YellowCounter.FileSystemState;

public class FileSystemStateSerializableTests
{
    [Fact]
    public void RoundTripDoesNotAffectOriginalTest()
    {
        string currentDir = Utility.GetRandomDirectory();
        string fileName = Path.GetRandomFileName() + ".txt";
        string fullName = Path.Combine(currentDir, fileName);

        FileSystemState state = new FileSystemState(currentDir, "*.csv");
        FileSystemState state2 = new FileSystemState(currentDir, "*.txt");

        RoundTrip(state, state2);

        using (var file = File.Create(fullName)) { }

        try
        {
            Assert.Empty(state.GetChanges());
            Assert.Single(state2.GetChanges());
        }
        finally
        {
            Directory.Delete(currentDir, true);
        }
    }

    private static void RoundTrip(FileSystemState source, FileSystemState destination)
    {
        source.LoadState();

        using (MemoryStream stream = new MemoryStream())
        {
            source.SaveState(stream);

            stream.Position = 0;
            destination.LoadState(stream);
        }
    }
}
