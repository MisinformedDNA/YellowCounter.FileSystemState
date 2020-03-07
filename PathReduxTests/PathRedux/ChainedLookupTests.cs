using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;
using Shouldly;

namespace PathReduxTests.PathRedux
{
    [TestClass]
    public class HashBucketTests
    {
        [TestMethod]
        public void HashBucketStoreRetrieve()
        {
            var m = new HashBucket(2, 2);

            m.Store(0, 123456).ShouldBe(true);
            m.Store(0, 765432).ShouldBe(true);

            var result = m.Retrieve(0);

            result.ToArray().ShouldBe(new[] { 123456, 765432 });
        }

        [TestMethod]
        public void HashBucketStoreFlowpast()
        {
            var m = new HashBucket(2, 2);

            m.Store(1, 123456).ShouldBe(true);
            m.Store(1, 765432).ShouldBe(false);

            var result = m.Retrieve(1);

            result.ToArray().ShouldBe(new[] { 123456 });
        }

        [TestMethod]
        public void HashBucketStoreZero()
        {
            var m = new HashBucket(2, 2);

            // It can store a zero
            m.Store(0, 0).ShouldBe(true);

            var result = m.Retrieve(0);
            result.ToArray().ShouldBe(new[] { 0 });
        }

        [TestMethod]
        public void HashBucketChainLimit()
        {
            var m = new HashBucket(8, 2);

            m.Store(0, 100).ShouldBe(true);
            m.Store(0, 200).ShouldBe(true);
            m.Store(0, 300).ShouldBe(false);

            var result = m.Retrieve(0);

            result.ToArray().ShouldBe(new[] { 100, 200 });
        }

        [TestMethod]
        public void HashBucketOverlap()
        {
            var m = new HashBucket(8, 8);

            // The values are going to overlap.
            m.Store(0, 100).ShouldBe(true);
            m.Store(1, 200).ShouldBe(true);
            m.Store(0, 300).ShouldBe(true);

            var result = m.Retrieve(0);

            result.ToArray().ShouldBe(new[] { 100, 200, 300 });
        }

        [TestMethod]
        public void HashBucketOverlapLimited()
        {
            var m = new HashBucket(8, 2);

            // If we set the max chain to a lower value then the overlap
            // won't occur.
            m.Store(0, 100).ShouldBe(true);
            m.Store(1, 200).ShouldBe(true);
            m.Store(0, 300).ShouldBe(false);

            m.Retrieve(0).ToArray().ShouldBe(new[] { 100, 200 });
            m.Retrieve(1).ToArray().ShouldBe(new[] { 200 });
        }
    }
}
