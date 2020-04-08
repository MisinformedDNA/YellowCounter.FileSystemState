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
            // Trying to trigger it rebuilding the text -> character buffer
            var ps = new PathStorage(new PathStorageOptions()
            {
                HashFunction = new DeterministicHashFunction(),
                InitialCharCapacity = 4,
                InitialHashCapacity = 2,
                LinearSearchLimit = 128,
                HashBucketMaxChain = 128,
                HashBucketInitialCapacity = 2,
            });

            var results = new List<int>();

            results.Add(ps.Store(@"C:\abc"));
            results.Add(ps.Store(@"C:\abc\xyz"));
            results.Add(ps.Store(@"C:\abc\cde"));
            results.Add(ps.Store(@"C:\mmm\cde"));
            results.Add(ps.Store(@"C:\abc"));

            ps.CreateString(results[0]).ShouldBe(@"C:\abc");
            ps.CreateString(results[1]).ShouldBe(@"C:\abc\xyz");
            ps.CreateString(results[2]).ShouldBe(@"C:\abc\cde");
            ps.CreateString(results[3]).ShouldBe(@"C:\mmm\cde");
            results[4].ShouldBe(results[0]);
        }

        [TestMethod]
        public void PathStorage2()
        {
            // Trying to trigger it rebuilding the text -> character buffer
            var ps = new PathStorage(new PathStorageOptions()
            {
                HashFunction = new DeterministicHashFunction(),
                InitialCharCapacity = 4,
                InitialHashCapacity = 2,
                LinearSearchLimit = 128,
                HashBucketMaxChain = 128,
                HashBucketInitialCapacity = 2,
            });

            var results = new List<int>();

            results.Add(ps.Store(@"C:\abc"));
            results.Add(ps.Store(@"C:\abc\xyz"));
            results.Add(ps.Store(@"C:\abc"));

            ps.CreateString(results[0]).ShouldBe(@"C:\abc");
            results[2].ShouldBe(results[0]);
        }
    }
}
