using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using YellowCounter.FileSystemState.PathRedux;
using Shouldly;

namespace YellowCounter.FileSystemState.Tests.PathRedux
{
    [TestClass]
    public class CharBufferTests
    {
        [TestMethod]
        public void CharBuffer1()
        {
            var charBuffer = new CharBuffer(100);

            int idx1 = charBuffer.Store("Hello");
            int idx2 = charBuffer.Store("World");

            charBuffer.Retrieve(idx1).ToString().ShouldBe("Hello");
            charBuffer.Retrieve(idx2).ToString().ShouldBe("World");
        }

        [TestMethod]
        public void CharBuffer2()
        {
            var charBuffer = new CharBuffer(100);

            int idx1 = charBuffer.Store("Hello");
            int idx2 = charBuffer.Store("World");

            charBuffer.Retrieve(new[] { idx1, idx2 }).ToString().ShouldBe("HelloWorld");
        }

        [TestMethod]
        public void CharBufferRealloc()
        {
            var charBuffer = new CharBuffer(13);

            int idx1 = charBuffer.Store("Hello");
            int idx2 = charBuffer.Store("World");

            var helloSpan = charBuffer.Retrieve(idx1);

            var worldSpan = charBuffer.Retrieve(idx2);

            charBuffer.Resize(25);

            // These spans are still pointing at the old buffer - how does it avoid
            // freeing up the memory?
            helloSpan.ToString().ShouldBe("Hello");
            worldSpan.ToString().ShouldBe("World");

            var hello2Span = charBuffer.Retrieve(idx1);
            var world2Span = charBuffer.Retrieve(idx2);

            hello2Span.ToString().ShouldBe("Hello");
            world2Span.ToString().ShouldBe("World");
        }

        [TestMethod]
        public void CharBufferEnumerate()
        {
            var charBuffer = new CharBuffer(100);

            int idx1 = charBuffer.Store("Hello");
            int idx2 = charBuffer.Store("World");

            var results = new List<string>();
            foreach(var item in charBuffer)
            {
                results.Add(item.Span.ToString());
            }

            results.ShouldBe(new[] { "Hello", "World" });
        }

        [TestMethod]
        public void CharBufferMaxCapacity()
        {
            // To store the text "Hello" without expanding, we need 5 chars for Hello,
            // 1 char for the null terminator of Hello, and 1 char for the null terminator
            // of the overall buffer.
            var charBuffer = new CharBuffer(7);

            int idx1 = charBuffer.Store("Hello");
            idx1.ShouldNotBe(-1);
            charBuffer.Capacity.ShouldBe(7);

            charBuffer.Retrieve(idx1).ToString().ShouldBe("Hello");

            int c = 0;
            foreach(var itm in charBuffer)
            {
                if(c == 0)
                {
                    itm.Pos.ShouldBe(0);
                    itm.Span.ToString().ShouldBe("Hello");
                }
                else
                {
                    throw new Exception("Should only have one item");
                }
                c++;
            }
        }
    }
}
