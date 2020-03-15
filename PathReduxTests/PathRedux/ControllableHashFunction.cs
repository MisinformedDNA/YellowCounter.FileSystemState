using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;

namespace PathReduxTests.PathRedux
{

    /// <summary>
    /// This hash function allows us to fix the hashcode to known values, based on
    /// the number before the comma.
    /// 
    /// The string must be in the format:
    /// "99999,Something"
    /// "99999,Another thing"
    /// Both these will get a hashcode of 99999.
    /// 
    /// Using this we can deliberately create hash collisions.
    /// </summary>
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
