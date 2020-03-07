using System;

namespace YellowCounter.FileSystemState.PathRedux
{
    public interface IHashFunction
    {
        int HashSequence(ReadOnlySpan<char> arg);
    }
}