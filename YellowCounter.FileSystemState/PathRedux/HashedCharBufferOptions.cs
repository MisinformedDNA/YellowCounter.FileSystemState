using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.HashCodes;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class HashedCharBufferOptions
    {
        public Func<IHashCode> NewHashCode { get; set; }
        public int InitialCharCapacity { get; set; }
        public int InitialHashCapacity { get; set; }
        public int LinearSearchLimit { get; set; }
    }
}
