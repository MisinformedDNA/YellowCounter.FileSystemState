using System;
using System.Collections.Generic;

namespace YellowCounter.FileSystemState.PathRedux
{
    public interface IHashFunction
    {
        int HashSequence(ReadOnlySpan<char> arg);
    }
}