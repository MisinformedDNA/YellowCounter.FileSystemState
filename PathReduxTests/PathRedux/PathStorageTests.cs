using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;
using Shouldly;

namespace YellowCounter.FileSystemState.Tests.PathRedux
{
    [TestClass]
    public class PathStorageTests
    {
        [TestMethod]
        public void PathStorage1()
        {
            var pathStorage = new PathStorage();

            int idx1 = pathStorage.Store("Hello");
            int idx2 = pathStorage.Store("World");

            pathStorage.Retrieve(idx1).ToString().ShouldBe("Hello");
            pathStorage.Retrieve(idx2).ToString().ShouldBe("World");
        }

        [TestMethod]
        public void PathStorage2()
        {
            var pathStorage = new PathStorage();

            int idx1 = pathStorage.Store("Hello");
            int idx2 = pathStorage.Store("World");

            pathStorage.Retrieve(new[] { idx1, idx2 }).ToString().ShouldBe("HelloWorld");
        }
    }
}
