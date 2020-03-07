using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;

namespace PathReduxTests.PathRedux
{
    internal class FixedHashFunction : IHashFunction
    {
        private Dictionary<string, int> _force = new Dictionary<string, int>();

        public FixedHashFunction Fix(string arg, int value)
        {
            _force[arg] = value;
            return this;
        }

        public int HashSequence(ReadOnlySpan<char> arg)
        {
            // Yes I know we are allocating a string here; this code is for testing
            // not performance.
            if(_force.TryGetValue(arg.ToString(), out int forcedHash))
                return forcedHash;

            throw new Exception($"Need a fixed hash value for {arg.ToString()}");
        }
    }
}
