using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;

namespace PathReduxTests.PathRedux
{
    public class ControllableHashFunction : IHashFunction
    {
        public int HashSequence(ReadOnlySpan<char> arg)
        {
            // Use comma as delimiter between desired hash number and remaining text.
            int commaPos = arg.IndexOf(',');

            if(commaPos == -1)
                throw new Exception($"{nameof(ControllableHashFunction)} requires , in each string");

            if(int.TryParse(arg.Slice(0, commaPos), out int result))
                return result;

            throw new Exception("Text before , must be an integer");
        }
    }
}
