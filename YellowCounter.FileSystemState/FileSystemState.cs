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

            FileChangeList rawChanges = GetCreatesAndChanges();

            var removals = GetRemovalsX(); //.ToLookup(x => (x.LastWriteTimeUtc, x.Length));

            // Look at all created files.
            // Can we find removed files which match on lastwrite / length?
            // These are probably renames.
            // TODO - same directory different name OR same name different directory
            var renames = rawChanges
                .Where(x => x.ChangeType == WatcherChangeTypes.Created)
                .Select(x => new {
                    FileChange = x,
                    State = _state.Get(x.Directory, x.Name)
                })
                .GroupJoin(removals,
                    x => new { x.State.LastWriteTimeUtc, x.State.Length },
                    x => new { x.LastWriteTimeUtc, x.Length },
                    (x, y) => new { NewFile = x, OldFile = y.First() })
                .ToList();

            var adds = rawChanges
                .Where(x => x.ChangeType == WatcherChangeTypes.Created)
                .Except(renames.Select(x => x.NewFile.FileChange));

            var removes = removals.Except(renames.Select(x => x.OldFile))
                .Select(x => new FileChange(x.Directory, x.Path, WatcherChangeTypes.Deleted));



            GetRenames(rawChanges);

            //List<(string directory, string path)> removals = GetRemovals();
            //foreach(var (directory, path) in removals)
            //{
            //    rawChanges.AddRemoved(directory, path);
            //    _state.Remove(directory, path);
            //}

            // Clear out the files that have been removed or renamed from our state.
            foreach(var r in removals)
            {
                _state.Remove(r.Directory, r.Path);
            }

            return rawChanges;
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

        private void GetRenames(FileChangeList changes)
        {

            foreach(var value in _state.Values)
            {
                // Find files in our state that have not been marked (have gone missing)
                if(value.Version != _version)
                {
                    // Is there another file in there with the same lastwrite and length?
                    // That's what we've renamed it to.
                    var renamedTo = _state.Keys
                        //.Where(x => x.directory == value.Directory)
                        .Select(x => _state[x])
                        .Where(x => x.LastWriteTimeUtc == value.LastWriteTimeUtc && x.Length == value.Length
                                    && (x.Path != value.Path || x.Directory != value.Directory))
                        .FirstOrDefault();

                   // changes.Remove(
                    _state.Remove(value.Directory, value.Path);
                }
            }
        }

        private IEnumerable<FileState> GetRemovalsX()
        {
            foreach(var value in _state.Values)
            {
                if(value.Version != _version)
                {
                    yield return value;
                }
            }
        }

        private List<(string directory, string path)> GetRemovals()
        {
            List<(string, string)> removals = new List<(string, string)>();
            foreach (var value in _state.Values)
            {
                if (value.Version != _version)
                {
                    removals.Add((value.Directory, value.Path));
                }
            }

            return removals;
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
