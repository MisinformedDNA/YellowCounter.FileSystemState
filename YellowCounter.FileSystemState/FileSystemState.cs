using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

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

            var enumerator = new FileSystemChangeEnumerator(this);
            while (enumerator.MoveNext())
            {
                // Ignore `.Current`
            }
            var changes = enumerator.Changes;

            List<(string directory, string path)> removals = GetRemovals();
            foreach (var (directory, path) in removals)
            {
                changes.AddRemoved(directory, path);
                _state.Remove(directory, path);
            }

            return changes;
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
