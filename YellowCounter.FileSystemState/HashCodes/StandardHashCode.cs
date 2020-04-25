using System;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.HashCodes
{
    public struct StandardHashCode : IHashCode
    {
        private HashCode hashCode;
        public void Add(char value)
        {
            hashCode.Add(value);
        }

        public int ToHashCode() => hashCode.ToHashCode();
    }
}
