using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.HashCodes
{
    public static class HashCodeExtensions
    {
        public static int HashSequence(this IHashCode hashCode, ReadOnlySpan<char> span)
        {
            foreach(var elem in span)
            {
                hashCode.Add(elem);
            }

            return hashCode.ToHashCode();
        }

        public static int HashSequence(this IHashCode hashCode, ReadOnlySequence<char> seq)
        {
            foreach(var mem in seq)
            {
                foreach(var elem in mem.Span)
                {
                    hashCode.Add(elem);
                }
            }

            return hashCode.ToHashCode();
        }
    }
}
