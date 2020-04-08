using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Linq;
using YellowCounter.FileSystemState.PathRedux;

namespace YellowCounter.FileSystemState
{
    public class FileSystemState : IAcceptFileSystemEntry 
    {
        private long _version = 0L;
        private PathToFileStateHashtable _state;

        public FileSystemState(string rootDir, string filter = "*", EnumerationOptions options = null)
        {
            this.RootDir = rootDir ?? throw new ArgumentNullException(nameof(rootDir));
            this.Filter = filter ?? throw new ArgumentNullException(nameof(filter));

            if(!Directory.Exists(rootDir))
                throw new DirectoryNotFoundException();

            EnumerationOptions = options ?? new EnumerationOptions();

            this.pathStorage = new PathStorage(new PathStorageOptions()
            {
                HashFunction = new HashFunction(),
                InitialCharCapacity = 1024,
                InitialHashCapacity = 256,
                LinearSearchLimit = 128,
                HashBucketMaxChain = 128,
                HashBucketInitialCapacity = 64
            });

            _state = new PathToFileStateHashtable(this.pathStorage);
        }

        public string RootDir { get; set; }
        public string Filter { get; set; }
        public EnumerationOptions EnumerationOptions { get; set; }

        private readonly PathStorage pathStorage;

        public void LoadState()
        {
            // Set initial baseline by reading current directory state without returning
            // every file as a change.
            gatherChanges();
            acceptChanges();
        }

        public void LoadState(Stream stream)
        {
            //BinaryFormatter serializer = new BinaryFormatter();
            //_state = (PathToFileStateHashtable)serializer.Deserialize(stream);
        }

        public void SaveState(Stream stream)
        {
            //BinaryFormatter serializer = new BinaryFormatter();
            //serializer.Serialize(stream, _state);
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
                this.RootDir, 
                this.EnumerationOptions,
                this);

            enumerator.Scan();
        }

        public void Accept(ref FileSystemEntry fileSystemEntry)
        {
            _state.Mark(ref fileSystemEntry, _version);
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
                .Select(x => newFileChange(x.DirectoryRef, x.FilenameRef, WatcherChangeTypes.Created))
                ;

            var changeResults = changes
                .Select(x => newFileChange(x.DirectoryRef, x.FilenameRef, WatcherChangeTypes.Changed))
                ;

            var removeResults = removals
                .Except(renames.Select(x => x.OldFile))
                .Select(x => newFileChange(x.DirectoryRef, x.FilenameRef, WatcherChangeTypes.Deleted))
                ;

            var renameResults = renames.Select(x => newFileChange2(
                x.NewFile.DirectoryRef,
                x.NewFile.FilenameRef,
                WatcherChangeTypes.Renamed,
                x.OldFile.DirectoryRef,
                x.OldFile.FilenameRef))
                ;
            
            var result = new FileChangeList();

            result.AddRange(createResults);
            result.AddRange(changeResults);
            result.AddRange(removeResults);
            result.AddRange(renameResults);

            return result;

            FileChange newFileChange(
                int directoryRef, 
                int filenameRef,
                WatcherChangeTypes changeType)
            {
                return new FileChange(
                    pathStorage.CreateString(directoryRef),
                    pathStorage.CreateString(filenameRef),
                    changeType);
            }

            FileChange newFileChange2(
                int newDirectoryRef,
                int newFilenameRef,
                WatcherChangeTypes changeType,
                int oldDirectoryRef,
                int oldFilenameRef
                )
            {
                return new FileChange(
                    pathStorage.CreateString(newDirectoryRef),
                    pathStorage.CreateString(newFilenameRef),
                    changeType,
                    pathStorage.CreateString(oldDirectoryRef),
                    pathStorage.CreateString(oldFilenameRef)
                    );
            }
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
                if(x.Flags.HasFlag(FileStateFlags.Seen))
                {
                    if(x.Flags.HasFlag(FileStateFlags.Created))
                        creates.Add(x);
                    else if(x.Flags.HasFlag(FileStateFlags.Changed))
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
                    Name = byName ? x.DirectoryRef : x.FilenameRef
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
                .GroupBy(x => new { x.LastWriteTimeUtc, x.Length, Name = byName ? x.DirectoryRef : x.FilenameRef },
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
