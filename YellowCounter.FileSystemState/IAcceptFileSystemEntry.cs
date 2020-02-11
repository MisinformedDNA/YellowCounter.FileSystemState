using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Text;

namespace YellowCounter.FileSystemState
{
    public interface IAcceptFileSystemEntry
    {
        void Accept(ref FileSystemEntry fileSystemEntry);
    }
}
