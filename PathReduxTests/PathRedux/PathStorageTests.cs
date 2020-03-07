using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;
using Shouldly;

namespace PathReduxTests.PathRedux
{
    [TestClass]
    public class PathStorageTests
    {
        [TestMethod]
        public void PathStorage1()
        {
            var ps = new PathStorage();

            var results = new List<int>();

            results.Add(ps.Store(@"C:\abc"));
            results.Add(ps.Store(@"C:\abc\xyz"));
            results.Add(ps.Store(@"C:\abc\cde"));
            results.Add(ps.Store(@"C:\mmm\cde"));

            ps.CreateString(results[0]).ShouldBe(@"C:\abc");
            ps.CreateString(results[1]).ShouldBe(@"C:\abc\xyz");
            ps.CreateString(results[2]).ShouldBe(@"C:\abc\cde");
            ps.CreateString(results[3]).ShouldBe(@"C:\mmm\cde");
        }
    }
}
