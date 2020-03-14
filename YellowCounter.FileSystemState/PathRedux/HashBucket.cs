using System;
using System.Buffers;
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
            mem = new int[capacity + maxChain];
            usage = new BitArray(capacity);

            this.capacity = capacity;
            this.maxChain = maxChain;
        }

        public int Capacity => this.capacity;
        public int MaxChain => this.maxChain;

        public bool Store(int hash, int value)
        {
            int bucket = bucketFromHash(hash);

            var span = mem.Span;

            for(int c = 0; c < maxChain; c++)
            {
                int i = bucket + c;
                int j = i % capacity;

                bool wrapAround = i != j;

                if(!usage[j])
                {
                    span[j] = value;
                    usage[j] = true;

                    // If wrapping around we have two copies of the values,
                    // one at the normal position and one in the runoff area
                    // at the end of the memory buffer.
                    // This so we have a contiguous span to slice for the
                    // return.
                    if(wrapAround)
                    {
                        span[i] = value;
                    }

                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// Modulo divide the hash by our capacity
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        private int bucketFromHash(int hash) => (int)unchecked((uint)hash % (uint)Capacity);


        public ReadOnlySpan<int> Retrieve(int hash)
        {
            int bucket = bucketFromHash(hash);

            var span = mem.Span;

            int c = 0;

            while(c < maxChain)
            {
                int j = (bucket + c) % capacity;

                if(!usage[j])
                    break;

                c++;
            }

            return span.Slice(bucket, c);
        }

    }
}
