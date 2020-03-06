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
    }
}
