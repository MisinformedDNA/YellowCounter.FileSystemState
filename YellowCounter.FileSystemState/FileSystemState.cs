using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;

namespace YellowCounter.FileSystemState
{
    public class FileSystemState : IAcceptFileSystemEntry 
    {
        private long _version = 0L;
        private PathToFileStateHashtable _state = new PathToFileStateHashtable(new StringInternPool());

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
            // Get the raw file changes, either create, file change or removal.
            var (creates, changes, removals) = getFileChanges();

            // Match up the creates and removals to get the renames
            var renames = matchRenames(creates, removals);

            // Convert to the output format.
            var result = convertToFileChanges(creates, changes, removals, renames);

            return result;
        }


        private void gatherChanges()
        {
            var enumerator = new FileSystemChangeEnumerator(
                this.Filter,
                this.Path, 
                this.EnumerationOptions,
                this);

            enumerator.Scan();
        }

        public void Accept(ref FileSystemEntry fileSystemEntry)
        {
            _state.Mark(ref fileSystemEntry, _version);

            //string path = fileSystemEntry.FileName.ToString();

            //FileState fs = new FileState();
            //fs.Directory = fileSystemEntry.Directory.ToString();
            //fs.Path = path;
            //fs.LastWriteTimeUtc = fileSystemEntry.LastWriteTimeUtc;
            //fs.Length = fileSystemEntry.Length;

            //_state.Mark(fs, _version);

        }

        private void acceptChanges()
        {
            // Clear out the files that have been removed or renamed from our state.
            _state.Sweep(_version);
            _version++;
        }

        private FileChangeList convertToFileChanges(
            IEnumerable<FileState> creates, 
            IEnumerable<FileState> changes, 
            IEnumerable<FileState> removals, 
            IEnumerable<(FileState NewFile, FileState OldFile)> renames)
        {
            var createResults = creates
                .Except(renames.Select(x => x.NewFile))
                .Select(x => new FileChange(x.Directory, x.Path, WatcherChangeTypes.Created))
                ;

            var changeResults = changes
                .Select(x => new FileChange(x.Directory, x.Path, WatcherChangeTypes.Changed))
                ;

            var removeResults = removals
                .Except(renames.Select(x => x.OldFile))
                .Select(x => new FileChange(x.Directory, x.Path, WatcherChangeTypes.Deleted))
                ;

            var renameResults = renames.Select(x => new FileChange(
                x.NewFile.Directory,
                x.NewFile.Path,
                WatcherChangeTypes.Renamed,
                x.OldFile.Directory,
                x.OldFile.Path))
                ;
            
            var result = new FileChangeList();

            result.AddRange(createResults);
            result.AddRange(changeResults);
            result.AddRange(removeResults);
            result.AddRange(renameResults);

            return result;
        }

        private (
            IEnumerable<FileState> creates,
            IEnumerable<FileState> changes,
            IEnumerable<FileState> removals) getFileChanges()
        {
            var creates = new List<FileState>();
            var changes = new List<FileState>();
            var removals = new List<FileState>();

            gatherChanges();

            foreach(var x in _state.Read())
            {
                if(x.LastSeenVersion == _version)
                {
                    if(x.CreateVersion == _version)
                        creates.Add(x);
                    else
                        changes.Add(x);
                }
                else
                    removals.Add(x);
            }

            acceptChanges();

            return (creates, changes, removals);
        }

        private IEnumerable<(FileState NewFile, FileState OldFile)> matchRenames(
            IEnumerable<FileState> creates,
            IEnumerable<FileState> removals)
        {
            // Want to match creates and removals to convert to renames either by:
            // Same directory, different name
            // or different directory, same name.
            return matchRenames(creates, removals, false)
                .Concat(matchRenames(creates, removals, true));
        }

        private IEnumerable<(FileState NewFile, FileState OldFile)> matchRenames(
            IEnumerable<FileState> creates,
            IEnumerable<FileState> removals,
            bool byName)
        {
            var createsByTime = creates
                .GroupBy(x => new
                {
                    // Group by last write time, length and directory or filename
                    x.LastWriteTimeUtc,
                    x.Length,
                    Name = byName ? x.Directory : x.Path
                },
                    (x, y) => new
                    {
                        // Return key fields, and list of all created files for the
                        // given (time, length, path) key
                        x.LastWriteTimeUtc,
                        x.Length,
                        x.Name,
                        Creates = y.ToList()
                    })
                .ToList();

            var removesByTime = removals
                .GroupBy(x => new { x.LastWriteTimeUtc, x.Length, Name = byName ? x.Directory : x.Path },
                (x, y) => new { x.LastWriteTimeUtc, x.Length, x.Name, Removes = y.ToList() })
                .ToList();

            // Join creates and removes by (time, length, directory), then filter to
            // only those matches which are unambiguous.
            return createsByTime.Join(removesByTime,
                x => new { x.LastWriteTimeUtc, x.Length, x.Name },
                x => new { x.LastWriteTimeUtc, x.Length, x.Name },
                (x, y) => new { x.Creates, y.Removes }
                )
                .Where(x => x.Creates.Count == 1 && x.Removes.Count == 1)
                .Select(x => (
                    NewFile: x.Creates[0],
                    OldFile: x.Removes[0]
                ))
                .ToList();
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
