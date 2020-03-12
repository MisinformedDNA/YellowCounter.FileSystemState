using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class PathStorage : IPathStorage
    {
        private HashedCharBuffer buf;
        private HashBucket buckets;
        private List<Entry> entries;

        public PathStorage()
        {
            buf = new HashedCharBuffer(new HashedCharBufferOptions()
            {
                HashFunction = new HashFunction(),
                InitialCharCapacity = 1024,
                InitialHashCapacity = 256,
                LinearSearchLimit = 128
            });

            buckets = new HashBucket(128, 16);

            entries = new List<Entry>();

            // Create a root entry so 0 is not a valid index
            entries.Add(new Entry(-1, -1));
        }

        public int Store(ReadOnlySpan<char> arg)
        {
            var hash = arg.GetHashOfContents();

            foreach(var idx in buckets.Retrieve(hash))
            {
                if(match(idx, arg))
                    return idx;
            }

            // Find a slash or backslash.
            int slashPos = arg.LastIndexOfAny(new[] { '\\', '/' });

            int parentIdx;
            int textRef;

            // No more slash delimiters, so store a root entry (parent index 0).
            if(slashPos == -1)
            {
                parentIdx = 0;
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

            return result;
        }

        public string CreateString(int idx)
        {
            return buf.CreateString(chain(idx));
        }

        private IEnumerable<int> chain(int idx)
        {
            int cursorIdx = idx;

            while(cursorIdx != 0)
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

                if(cursorIdx == 0)
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
