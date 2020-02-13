using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState
{
    public static class ReadOnlySpanExtensions
    {
        /// <summary>
        /// Combine hashcodes of each element in the ReadOnlySpan
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="span"></param>
        /// <returns></returns>
        public static int GetHashOfContents<T>(this ReadOnlySpan<T> span)
        {
            // struct so allocated on stack
            var hash = new HashCode();

            foreach(var elem in span)
            {
                hash.Add(elem);
            }

            return hash.ToHashCode();
        }
    }
}
