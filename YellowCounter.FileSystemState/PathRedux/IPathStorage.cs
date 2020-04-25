using System;

namespace YellowCounter.FileSystemState.PathRedux
{
    public interface IPathStorage
    {
        string CreateString(int idx);
        int Store(ReadOnlySpan<char> arg);
    }
}