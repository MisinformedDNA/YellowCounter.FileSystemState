using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class PathToFileStateHashtable
    {
        HashSet<FileState> hash;
        public PathToFileStateHashtable() 
        {
            hash = new HashSet<FileState>(100, new FileStateComparer());
        }

        public void Mark(FileState input, long version)
        {
            // Is the file already known to us?
            if(hash.TryGetValue(input, out FileState fs))
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
            else // It's a new file.
            {
                // Don't futz the input, clone it
                FileState fs2 = input.Clone();

                // Mark that we've seen it
                fs2.LastSeenVersion = version;
                fs2.CreateVersion = version;
                fs2.ChangeVersion = version;

                hash.Add(fs2);
            }
        }

        internal void Sweep(long version)
        {
            // Remove the records of files that have been deleted.
            hash.RemoveWhere(x => x.LastSeenVersion != version);
        }

        public IEnumerable<FileState> Read()
        {
            foreach(var x in hash)
                yield return x;
        }
    }

    internal class FileStateComparer : IEqualityComparer<FileState>
    {
        // Equivalent if directory and path match.
        public bool Equals(FileState x, FileState y)
        {
            return x.Directory == y.Directory && x.Path == y.Path;
        }

        public int GetHashCode(FileState obj) =>
            HashCode.Combine(obj.Directory.GetHashCode() ^ obj.Path.GetHashCode());
    }
}
