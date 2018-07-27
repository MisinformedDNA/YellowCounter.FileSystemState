[![NuGet](https://img.shields.io/nuget/v/YellowCounter.FileSystemState.svg)](https://www.nuget.org/packages/YellowCounter.FileSystemState/)

# YellowCounter.FileSystemState
Like FileSystemWatcher except you control when the state is checked. This allows it to work well for scheduled tasks instead of relying on continuous jobs.

## Sample

```csharp
    class Program
    {
        const string FileName = "state.bin";
        static void Main(string[] args)
        {
            var state = new FileSystemState(Environment.CurrentDirectory, "*.txt", new EnumerationOptions { RecurseSubdirectories = true });

            if (File.Exists(FileName))
            {
                // If the file exists, get file system state from the file
                using (var stream = File.OpenRead(FileName))
                {
                    state.LoadState(stream);
                }
            }
            else
            {
                // Otherwise, get the current file system state
                state.LoadState();
            }

            var changes = state.GetChanges();    // Looks for changes in file system state

            using (var stream = File.OpenWrite(FileName))
            {
                state.SaveState(stream);
            }
        }
    }
```
