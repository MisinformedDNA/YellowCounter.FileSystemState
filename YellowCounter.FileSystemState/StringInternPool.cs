using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState
{
    /// <summary>
    /// Not thread-safe string interning.
    /// Probably needs a garbage collector at some point?
    /// </summary>
    public class StringInternPool : IStringInternPool
    {
        public Dictionary<int, List<string>> dict = new Dictionary<int, List<string>>();

        public string Intern(ref ReadOnlySpan<char> span)
        {
            int hash = span.GetHashOfContents();

            if(dict.TryGetValue(hash, out var strings))
            {
                foreach(var s in strings)
                {
                    // Interned case - found existing string which matches.
                    if(span.Equals(s, StringComparison.Ordinal))
                        return s;
                }

                // Hash collision
                string newString = span.ToString();
                strings.Add(newString);

                return newString;
            }
            else
            {
                // Add new item
                string newString = span.ToString();

                var newList = new List<string>();
                newList.Add(newString);

                dict.Add(hash, newList);

                return newString;
            }
        }
    }
}
