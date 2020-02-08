using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace YellowCounter.FileSystemState
{
    public class FileSystemState
    {
        private long _version = default;
        private PathToFileStateHashtable _state = new PathToFileStateHashtable();

        public FileSystemState(string path, string filter = "*", EnumerationOptions options = null)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));

            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException();

            EnumerationOptions = options ?? new EnumerationOptions();
        }

        public string Path { get; set; }
        public string Filter { get; set; }
        public EnumerationOptions EnumerationOptions { get; set; }

        public void LoadState()
        {
            GetChanges();
        }

        public void LoadState(Stream stream)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            _state = (PathToFileStateHashtable)serializer.Deserialize(stream);
        }

        public void SaveState(Stream stream)
        {
            BinaryFormatter serializer = new BinaryFormatter();
            serializer.Serialize(stream, _state);
        }

        // This function walks all watched files, collects changes, and updates state
        public FileChangeList GetChanges()
        {
            _version++;

            var rawChanges = GetCreatesAndChanges();

            var removals = GetRemovals().ToList();

            var createsByTime = rawChanges
                .Where(x => x.ChangeType == WatcherChangeTypes.Created)
                .Select(x => new
                {
                    FileChange = x,
                    State = _state.Get(x.Directory, x.Name)
                })
                .GroupBy(x => new
                {
                    // Group by last write time, length and directory
                    x.State.LastWriteTimeUtc,
                    x.State.Length,
                    x.State.Directory
                },
                    (x, y) => new
                    {
                        // Return key fields, and list of all created files for the
                        // given (time, length, path) key
                        x.LastWriteTimeUtc,
                        x.Length,
                        x.Directory,
                        Creates = y.ToList()
                    })
                .ToList();

            var removesByTime = removals
                .GroupBy(x => new { x.LastWriteTimeUtc, x.Length, x.Directory },
                (x, y) => new { x.LastWriteTimeUtc, x.Length, x.Directory, Removes = y.ToList() })
                .ToList();

            // Join creates and removes by (time, length, directory), then filter to
            // only those matches which are unambiguous.
            var renames = createsByTime.Join(removesByTime,
                x => new { x.LastWriteTimeUtc, x.Length, x.Directory },
                x => new { x.LastWriteTimeUtc, x.Length, x.Directory },
                (x, y) => new { x.Creates, y.Removes }
                )
                .Where(x => x.Creates.Count == 1 && x.Removes.Count == 1)
                .Select(x => new
                {
                    NewFile = x.Creates[0],
                    OldFile = x.Removes[0]
                })
                .ToList();

            var adds = rawChanges
                .Where(x => x.ChangeType == WatcherChangeTypes.Created)
                .Except(renames.Select(x => x.NewFile.FileChange))
                .ToList();

            var changes = rawChanges
                .Where(x => x.ChangeType == WatcherChangeTypes.Changed)
                .ToList();

            var removes = removals
                .Except(renames.Select(x => x.OldFile))
                .Select(x => new FileChange(x.Directory, x.Path, WatcherChangeTypes.Deleted))
                .ToList();

            var renames2 = renames.Select(x => new FileChange(x.NewFile.FileChange.Directory,
                x.NewFile.FileChange.Name,
                WatcherChangeTypes.Renamed,
                x.OldFile.Directory,
                x.OldFile.Path))
                .ToList();


            // Clear out the files that have been removed or renamed from our state.
            foreach(var r in removals)
            {
                _state.Remove(r.Directory, r.Path);
            }

            var result = new FileChangeList();

            result.AddRange(adds);
            result.AddRange(changes);
            result.AddRange(removes);
            result.AddRange(renames2);

            return result;
        }

        private FileChangeList GetCreatesAndChanges()
        {
            var enumerator = new FileSystemChangeEnumerator(this);
            while(enumerator.MoveNext())
            {
                // Ignore `.Current`
            }
            var changes = enumerator.Changes;
            return changes;
        }

        private IEnumerable<FileState> GetRemovals()
        {
            foreach(var value in _state.Values)
            {
                if(value.Version != _version)
                {
                    yield return value;
                }
            }

        }


        protected internal virtual void DetermineChange(string directory, ref FileChangeList changes, ref FileSystemEntry file)
        {
            string path = file.FileName.ToString();

            FileState fileState = _state.Get(directory, path);
            if (fileState == null) // file added
            {
                fileState = new FileState();
                fileState.Directory = directory;
                fileState.Path = path;
                fileState.LastWriteTimeUtc = file.LastWriteTimeUtc;
                fileState.Length = file.Length;
                fileState.Version = _version;
                _state.Add(directory, path, fileState);
                changes.AddAdded(directory, path);
                return;
            }

            fileState.Version = _version;

            var previousState = fileState;
            if (file.LastWriteTimeUtc != fileState.LastWriteTimeUtc || file.Length != fileState.Length)
            {
                changes.AddChanged(directory, fileState.Path);
                fileState.LastWriteTimeUtc = file.LastWriteTimeUtc;
                fileState.Length = file.Length;
            }
        }

        protected internal virtual bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            if (entry.IsDirectory) return false;

            bool ignoreCase = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            if (FileSystemName.MatchesSimpleExpression(Filter, entry.FileName, ignoreCase: ignoreCase))
                return true;

            return false;
        }

        protected internal virtual bool ShouldRecurseIntoEntry(ref FileSystemEntry entry) => true;
    }
}
