using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.IO.Enumeration;
using YellowCounter.FileSystemState.PathRedux;
using System.Diagnostics;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class PathToFileStateHashtable
    {
        Dictionary<int, List<FileState>> dict;
        private readonly IPathStorage pathStorage;

        public PathToFileStateHashtable(IPathStorage pathStorage) 
        {
            dict = new Dictionary<int, List<FileState>>();

            this.pathStorage = pathStorage;
        }

        internal void Mark(ref FileSystemEntry input)
        {
            int dirRef = pathStorage.Store(input.Directory);
            int filenameRef = pathStorage.Store(input.FileName);

            int hashCode = HashCode.Combine(dirRef.GetHashCode(), filenameRef.GetHashCode());

            if(dict.TryGetValue(hashCode, out var fileStates))
            {
                bool found = false;

                // Normally there will only be 1 but we could get a hash collision.
                foreach(var existing in fileStates)
                {
                    // We've only matched on hashcode so far, so there could be false
                    // matches in here. Do a proper comparision on filename/directory.
                    if(existing.FilenameRef == filenameRef && existing.DirectoryRef == dirRef)
                    {
                        // Found the file; compare to our existing record so we can
                        // detect if it has been modified.
                        markExisting(existing, input);

                        found = true;
                        break;
                    }
                }

                // Hash collision! Add on the end of the list.
                if(!found)
                {
                    fileStates.Add(newFileState(input));
                }
            }
            else
            {
                // Not seen before, create a 1-element list and add to the dictionary.
                dict.Add(hashCode, new List<FileState>() { newFileState(input) });
            }

            FileState newFileState(FileSystemEntry input)
            {
                var fileState = new FileState();

                fileState.Flags = FileStateFlags.Created | FileStateFlags.Seen;

                fileState.DirectoryRef = dirRef;
                fileState.FilenameRef = filenameRef;

                fileState.LastWriteTimeUtc = input.LastWriteTimeUtc;
                fileState.Length = input.Length;

                return fileState;
            }
        }

        private void markExisting(FileState fs, FileSystemEntry input)
        {
            // Mark that we've seen the file.
            fs.Flags |= FileStateFlags.Seen;

            // Has it changed since we last saw it?
            if(fs.LastWriteTimeUtc != input.LastWriteTimeUtc
                || fs.Length != input.Length)
            {
                fs.Flags |= FileStateFlags.Changed;

                // Update the last write time / file length.
                fs.LastWriteTimeUtc = input.LastWriteTimeUtc;
                fs.Length = input.Length;
            }
        }



        public IEnumerable<FileState> Read()
        {
            foreach(var x in dict.Values.SelectMany(y => y))
            {
                yield return x;
            }
        }

        public void Sweep()
        {
            var toRemove = new List<int>();

            // Go through every list of filestates in our state dictionary
            foreach(var (hash, list) in dict)
            {
                // Remove any item in the list which we didn't see on the last mark
                // phase (every item that is seen gets the LastSeenVersion updated)
                //list.RemoveAll(x => x.LastSeenVersion != version);

                list.RemoveAll(x => !x.Flags.HasFlag(FileStateFlags.Seen));

                // In the normal case where there are no hash collisions, this will
                // remove the one and only item from the list. We can then remove
                // the hash entry from the dictionary.
                // If there was a hash collision, the reduced-size list would remain.
                if(list.Count == 0)
                {
                    toRemove.Add(hash);
                }

                // Clear the flags on all remaining items.
                foreach(var x in list)
                {
                    x.Flags = FileStateFlags.None;
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
