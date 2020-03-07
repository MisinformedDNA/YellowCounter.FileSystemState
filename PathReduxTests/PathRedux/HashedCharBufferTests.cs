using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
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
        public void HashedCharBufferAddAndRetrieveNoClash()
        {
            // Fix the hash codes.
            var hasher = new FixedHashFunction()
                .Fix("Hello", 1)
                .Fix("World", 2);

            var buf = new HashedCharBuffer(new HashedCharBufferOptions()
            {
                HashFunction = hasher,
                InitialCharCapacity = 20,
                InitialHashCapacity = 16,
                LinearSearchLimit = 3
            });

            buf.Store("Hello");
            buf.Store("World");

            buf.Find("Hello").ShouldBe(0);
            buf.Find("World").ShouldBe(6);

            buf.Retrieve(0).ToString().ShouldBe("Hello");
            buf.Retrieve(6).ToString().ShouldBe("World");
        }

        [TestMethod]
        public void HashedCharBufferAddAndRetrieveClash()
        {
            // Fix the hash codes to the same value
            var hasher = new FixedHashFunction()
                .Fix("Hello", 1)
                .Fix("World", 1);

            var buf = new HashedCharBuffer(new HashedCharBufferOptions()
            {
                HashFunction = hasher,
                InitialCharCapacity = 20,
                InitialHashCapacity = 16,
                LinearSearchLimit = 3
            });

            buf.Store("Hello");
            buf.Store("World");

            buf.Find("Hello").ShouldBe(0);
            buf.Find("World").ShouldBe(6);

            buf.Retrieve(0).ToString().ShouldBe("Hello");
            buf.Retrieve(6).ToString().ShouldBe("World");
        }

        [TestMethod]
        public void HashedCharBufferHashCollision()
        {
            // Fix the hash codes to the same value
            var hasher = new FixedHashFunction()
                .Fix("Hello", 1)
                .Fix("World", 1);

            // Allow only 1 item in the linear search phase
            var buf = new HashedCharBuffer(new HashedCharBufferOptions()
            {
                HashFunction = hasher,
                InitialCharCapacity = 20,
                InitialHashCapacity = 16,
                LinearSearchLimit = 1
            });

            buf.Store("Hello");

            Should.Throw(() =>
            {
                buf.Store("World");
            }, typeof(Exception)).Message.ShouldBe("Too many hash collisions. Increase LinearSearchLimit to overcome.");
        }

        [TestMethod]
        public void HashedCharBufferAddAndRetrieveClashRunOutX()
        {
            // Fix the hash codes to the same value modulo 16
            var hasher = new FixedHashFunction()
                .Fix("Hello", 1)
                .Fix("World", 17);

            // Allow 1 items in the linear search phase
            var buf = new HashedCharBuffer(new HashedCharBufferOptions()
            {
                HashFunction = hasher,
                InitialCharCapacity = 20,
                InitialHashCapacity = 16,
                LinearSearchLimit = 1
            });

            buf.HashCapacity.ShouldBe(16);

            buf.Store("Hello");
            buf.Store("World");

            buf.Find("Hello").ShouldBe(0);
            buf.Find("World").ShouldBe(6);

            buf.Retrieve(0).ToString().ShouldBe("Hello");
            buf.Retrieve(6).ToString().ShouldBe("World");

            // Hash capacity will have doubled to avoid clash of hashes
            // 1 % 16 and 17 % 16
            // Once we double, we get 32 hash buckets so clash avoided.
            buf.HashCapacity.ShouldBe(32);
        }
    }
}
