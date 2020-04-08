using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class PathStorage : IPathStorage
    {
        private IHashFunction hashFunction;
        private HashedCharBuffer buf;
        private HashBucket buckets;
        private List<Entry> entries;
        private const int Root = -1;    // The root entry's ParentIdx is set to this.

        public PathStorage(PathStorageOptions options)
        {
            this.hashFunction = options.HashFunction;

            buf = new HashedCharBuffer(new HashedCharBufferOptions()
            {
                HashFunction = options.HashFunction,
                InitialCharCapacity = options.InitialCharCapacity,
                InitialHashCapacity = options.InitialHashCapacity,
                LinearSearchLimit = options.LinearSearchLimit
            });

            buckets = new HashBucket(
                options.HashBucketInitialCapacity, 
                options.HashBucketMaxChain);

            entries = new List<Entry>();
        }

        public int Store(ReadOnlySpan<char> arg)
        {
            var hash = hashFunction.HashSequence(arg);

            foreach(var idx in buckets.Retrieve(hash))
            {
                if(match(idx, arg))
                {
                    Debug.WriteLine($"Found match for {arg.ToString()} ({hash})= {idx}");
                    return idx;
                }
            }

            // Find a slash or backslash.
            int slashPos = arg.LastIndexOfAny(new[] { '\\', '/' });

            int parentIdx;
            int textRef;

            // No more slash delimiters, so store a root entry (parent index -1).
            if(slashPos == -1)
            {
                parentIdx = Root;
                textRef = buf.Store(arg);
            }
            else
            {
                // Recursively call back to ourselves to store all text
                // up to the parent directory name. This might find an
                // existing entry or need to create one.
                parentIdx = this.Store(arg.Slice(0, slashPos));

                // Store the text from the slash onwards as our entry.
                textRef = buf.Store(arg.Slice(slashPos));
            }

            int result = entries.Count;
            entries.Add(new Entry(textRef, parentIdx));

            if(!buckets.Store(hash, result))
            {

                Debug.WriteLine($"Start rebuildBuckets");

                // Rebuild buckets from List<Entry> twice as big
                rebuildBuckets();

                Debug.WriteLine($"End rebuildBuckets");

                if(!buckets.Store(hash, result))
                    throw new Exception($"Too many hash collisions in {nameof(PathStorage)}");
            }

            Debug.WriteLine($"Created {arg.ToString()} ({hash})= {result}");

            return result;
        }

        private void rebuildBuckets()
        {
            var newBuckets = new HashBucket(buckets.Capacity * 2, buckets.MaxChain);

            for(int idx = 0; idx < entries.Count; idx++)
            {
                var h = new HashCode();

                foreach(var textRef in chain(idx).Reverse())
                {
                    var text = buf.Retrieve(textRef);
                    h.Add(hashFunction.HashSequence(text));
                }

                int hash = h.ToHashCode();

                newBuckets.Store(hash, idx);
            }

            this.buckets = newBuckets;
        }

        public int HashEntry(int idx)
        {
            var text = buf.Retrieve(chain(idx));
            
            return 0;
        }

        public string CreateString(int idx)
        {
            return buf.CreateString(chain(idx));
        }

        private IEnumerable<int> chain(int idx)
        {
            int cursorIdx = idx;

            while(cursorIdx != Root)
            {
                var entry = entries[cursorIdx];

                yield return entry.TextRef;
                cursorIdx = entry.ParentIdx;
            }
        }

        private bool match(int idx, ReadOnlySpan<char> arg)
        {
            int argStart = arg.Length;
            int cursorIdx = idx;

            while(true)
            {
                var entry = entries[cursorIdx];

                var text = buf.Retrieve(entry.TextRef);

                argStart -= text.Length;

                if(argStart < 0)
                    return false;

                var argSlice = arg.Slice(argStart, text.Length);

                if(!text.SequenceEqual(argSlice))
                    return false;

                // Loop round to our parent entry
                cursorIdx = entry.ParentIdx;

                if(cursorIdx == Root)
                {
                    // If the target has no parent, and we've examined all of arg
                    // then we've got a correct match
                    if(argStart == 0)
                        return true;

                    return false;
                }
            }

        }

        private readonly struct Entry
        {
            public Entry(int textRef, int parentIdx)
            {
                this.TextRef = textRef;
                this.ParentIdx = parentIdx;
            }

            public int TextRef { get; }
            public int ParentIdx { get; }
        }

    }
}
