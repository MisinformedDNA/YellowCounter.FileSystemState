using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class HashedCharBufferOptions
    {
        public IHashFunction HashFunction { get; set; }
        public int InitialCharCapacity { get; set; }
        public int InitialHashCapacity { get; set; }
        public int LinearSearchLimit { get; set; }
    }
}
