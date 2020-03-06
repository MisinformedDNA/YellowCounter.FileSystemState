using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class HashedCharBuffer
    {
        private readonly int linearSearchLimit;
        private CharBuffer charBuffer;
        private ChainedLookup chainedLookup;

        public HashedCharBuffer(int initialCharCapacity, int initialHashCapacity, int linearSearchLimit)
        {
            charBuffer = new CharBuffer(initialCharCapacity);
            chainedLookup = new ChainedLookup(initialHashCapacity, linearSearchLimit);
            this.linearSearchLimit = linearSearchLimit;
        }

        /// <summary>
        /// Returns index position
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public int Store(ReadOnlySpan<char> text)
        {
            int hash = text.GetHashOfContents();
            int foundPos = findByHash(hash, text);

            if(foundPos != -1)
                return foundPos;

            int pos = charBuffer.Store(text);
            if(pos == -1)
            {
                int newSize = charBuffer.Capacity * 2;
                if(newSize < text.Length + charBuffer.Capacity)
                    newSize = charBuffer.Capacity + text.Length;

                charBuffer.Resize(newSize);

                pos = charBuffer.Store(text);
            }

            if(!chainedLookup.Store(hash, pos))
            {
                rebuildLookup();
                chainedLookup.Store(hash, pos);
            }

            return pos;
        }

        public ReadOnlySpan<char> Retrieve(int pos)
        {
            return charBuffer.Retrieve(pos);
        }

        public int Find(ReadOnlySpan<char> text)
        {
            int hash = text.GetHashOfContents();
            return findByHash(hash, text);
        }

        private int findByHash(int hash, ReadOnlySpan<char> text)
        {
            var indices = chainedLookup.Retrieve(hash);
            return charBuffer.Match(text, indices);
        }

        private void rebuildLookup()
        {
            // Doubling capacity will halve the number of hash collisions
            var newLookup = new ChainedLookup(chainedLookup.Capacity * 2, linearSearchLimit);

            // Populate a new lookup from our existing data.
            foreach(var itm in charBuffer)
            {
                if(!newLookup.Store(itm.Span.GetHashOfContents(), itm.Pos))
                    throw new Exception("Oops");
            }

            // Use the new lookup
            chainedLookup = newLookup;
        }
    }
}





// Split by backslash / slash

// Starting at the longest sequence,
// e.g. C:\abc\cde\efg\ghi\
// then going backwards as
// C:\abc\cde\efg\
// C:\abc\cde\
// C:\abc\

// Generate the hashcode of the text.
// Look up the hashcode in the dictionary
// If we found it, we will get two things:
// Index of the tail entry
// Index of the parent

// Create a new record