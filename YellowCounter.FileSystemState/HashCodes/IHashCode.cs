using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.HashCodes
{
    public interface IHashCode
    {
        void Add(char value);
        int ToHashCode();
    }
}
