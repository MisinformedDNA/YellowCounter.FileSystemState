using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.IO.Enumeration;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class PathToFileStateHashtable
    {
        Dictionary<int, List<FileState>> dict;
        private readonly IStringInternPool stringInternPool;
        private readonly int truncate;

        public PathToFileStateHashtable(IStringInternPool stringInternPool, int truncate = 0) 
        {
            dict = new Dictionary<int, List<FileState>>();
            this.stringInternPool = stringInternPool;
            this.truncate = truncate;
        }

        internal void Mark(ref FileSystemEntry input,long version)
        {
            // If we are scanning folder c:\verylongdirectoryname\ there is no need to store
            // the same text c:\verylongdirectoryname\ over and over again so we remove the
            // root from the directory name leaving the relative path
            var relativeDir = input.Directory.Slice(truncate);

            // Without allocating strings, calculate a hashcode based on the
            // directory and filename.
            int hashCode = HashCode.Combine(
                relativeDir.GetHashOfContents(),
                input.FileName.GetHashOfContents());

            if(dict.TryGetValue(hashCode, out var fileStates))
            {
                bool found = false;

                // Normally there will only be 1 but we could get a hash collision.
                foreach(var existing in fileStates)
                {
                    // We've only matched on hashcode so far, so there could be false
                    // matches in here. Do a proper comparision on filename/directory.

                    // Use Equals() to match to avoid allocating strings.
                    if(input.FileName.Equals(existing.FileName, StringComparison.Ordinal)
                        && relativeDir.Equals(existing.RelativeDir, StringComparison.Ordinal))
                    {
                        // Found the file; compare to our existing record so we can
                        // detect if it has been modified.
                        markExisting(existing, input, version);

                        found = true;
                        break;
                    }
                }

                // Hash collision! Add on the end of the list.
                if(!found)
                {
                    fileStates.Add(newFileState(input, ref relativeDir, version));
                }
            }
            else
            {
                // Not seen before, create a 1-element list and add to the dictionary.
                dict.Add(hashCode, new List<FileState>() { newFileState(input, ref relativeDir, version) });
            }
        }

        private void markExisting(FileState fs, FileSystemEntry input, long version)
        {
            // Mark that we've seen the file.
            fs.LastSeenVersion = version;

            // Has it changed since we last saw it?
            if(fs.LastWriteTimeUtc != input.LastWriteTimeUtc
                || fs.Length != input.Length)
            {
                // Mark that this version was a change
                fs.ChangeVersion = version;

                // Update the last write time / file length.
                fs.LastWriteTimeUtc = input.LastWriteTimeUtc;
                fs.Length = input.Length;
            }
        }

        private FileState newFileState(FileSystemEntry input, ref ReadOnlySpan<char> relativeDir, long version)
        {
            var fileState = new FileState();

            fileState.LastSeenVersion = version;
            fileState.CreateVersion = version;
            fileState.ChangeVersion = version;

            // Here's where we're allocating the strings. Note we only do this when
            // we first see a file, not on each subsequent scan for changes.
            var fn = input.FileName;

            fileState.RelativeDir = stringInternPool.Intern(ref relativeDir);
            fileState.FileName = stringInternPool.Intern(ref fn);

            //fileState.Directory = input.Directory.ToString();
            //fileState.Path = input.FileName.ToString();

            fileState.LastWriteTimeUtc = input.LastWriteTimeUtc;
            fileState.Length = input.Length;

            return fileState;
        }

        public IEnumerable<FileState> Read()
        {
            foreach(var x in dict.Values.SelectMany(y => y))
            {
                yield return x;
            }
        }

        public void Sweep(long version)
        {
            var toRemove = new List<int>();

            // Go through every list of filestates in our state dictionary
            foreach(var (hash, list) in dict)
            {
                // Remove any item in the list which we didn't see on the last mark
                // phase (every item that is seen gets the LastSeenVersion updated)
                list.RemoveAll(x => x.LastSeenVersion != version);

                // In the normal case where there are no hash collisions, this will
                // remove the one and only item from the list. We can then remove
                // the hash entry from the dictionary.
                // If there was a hash collision, the reduced-size list would remain.
                if(list.Count == 0)
                {
                    toRemove.Add(hash);
                }
            }

            // We can't remove the items while iterating so remove here instead.
            foreach(var hash in toRemove)
            {
                dict.Remove(hash);
            }
        }
    }

}
