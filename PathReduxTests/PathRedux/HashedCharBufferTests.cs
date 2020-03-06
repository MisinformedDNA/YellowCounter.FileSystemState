using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;

namespace PathReduxTests.PathRedux
{
    [TestClass]
    public class HashedCharBufferTests
    {

        [TestMethod]
        public void HashedCharBufferAddAndRetrieve()
        {
            var buf = new HashedCharBuffer(20, 16, 3);

            buf.Store("Hello");
            buf.Store("World");

            buf.Find("Hello").ShouldBe(0);
            buf.Find("World").ShouldBe(6);

            buf.Retrieve(0).ToString().ShouldBe("Hello");
            buf.Retrieve(6).ToString().ShouldBe("World");
        }
    }
}
