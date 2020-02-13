using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Linq;
using YellowCounter.FileSystemState;

namespace YellowCounter.FileSystemState.Tests
{
    public class ReadOnlySpanCharHashing
    {
        [Fact]
        public void Test1()
        {
            var x = new ReadOnlySpan<char>("Hello".ToCharArray());
            var y = new ReadOnlySpan<char>("Hello".ToCharArray());

            // Note that each run of the program gets a new key so we can't rely
            // on a specific fixed value.
            Assert.Equal(x.GetHashOfContents(), y.GetHashOfContents());
        }
    }
}
