using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class HashBucket
    {
        private Memory<int> mem;
        private readonly int capacity;
        private readonly int maxChain;
        private BitArray usage;

        public HashBucket(int capacity, int maxChain)
        {
            mem = new int[capacity];
            usage = new BitArray(capacity);
            this.capacity = capacity;
            this.maxChain = maxChain;
        }

        public int Capacity => mem.Length;

        public bool Store(int hash, int value)
        {
            int key = keyFromHash(hash);

            var span = mem.Span;
            int chainLen = 0;

            // Look for an empty slot in our buffer
            for(int i = key; i < capacity; i++)
            {
                if(!usage[i])
                {
                    span[i] = value;
                    usage[i] = true;

                    return true;
                }

                chainLen++;
                
                // Don't build up too long a chain of values - we'll build a new
                // buffer instead.
                if(chainLen >= maxChain)
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Modulo divide the hash by our capacity
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private int keyFromHash(int hash) => (int)unchecked((uint)hash % (uint)Capacity);

        public ReadOnlySpan<int> Retrieve(int hash)
        {
            int key = keyFromHash(hash); 

            var span = mem.Span;
            int chainLen = 0;

            for(int i = key; i < capacity && chainLen <= maxChain; i++)
            {
                if(!usage[i])
                    break;

                chainLen++;
            }

            return mem.Span.Slice(key, chainLen);
        }
    }
}
