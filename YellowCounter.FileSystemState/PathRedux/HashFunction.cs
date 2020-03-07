using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class HashFunction : IHashFunction
    {
        public int HashSequence(ReadOnlySpan<char> arg) => arg.GetHashOfContents();
    }
}
