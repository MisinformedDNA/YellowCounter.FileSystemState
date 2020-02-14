using System;

namespace YellowCounter.FileSystemState
{
    public interface IStringInternPool
    {
        string Intern(ref ReadOnlySpan<char> span);
    }
}