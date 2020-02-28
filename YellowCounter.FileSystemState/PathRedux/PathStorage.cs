using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class PathStorage
    {
        private Memory<char> buffer = new char[100000];
        private int pos;

        public void Init()
        {
        }

        public int Store(ReadOnlySpan<char> input)
        {
            var bufSpan = buffer.Span;

            var result = pos;

            input.CopyTo(bufSpan.Slice(pos, input.Length));
            pos += input.Length;

            bufSpan[pos] = '\0';
            pos++;

            return result;
        }

        public ReadOnlySpan<char> Retrieve(int index)
        {
            var bufSpan = buffer.Span;

            var begin = bufSpan.Slice(index);

            int len = begin.IndexOf('\0');

            return begin.Slice(0, len);
        }

        public ReadOnlySequence<char> Retrieve(IEnumerable<int> indices)
        {
            Segment<char> root = null;
            Segment<char> current = null;

            int len = 0;

            foreach(var idx in indices)
            {
                var tail = buffer.Slice(idx);
                len = tail.Span.IndexOf('\0');
                var text = tail.Slice(0, len);

                if(root == null)
                {
                    root = new Segment<char>(text);
                    current = root;
                }
                else
                {
                    current = current.Add(text);
                }
            }

            return new ReadOnlySequence<char>(root, 0, current, len);
        }

        class Segment<T> : ReadOnlySequenceSegment<T>
        {
            public Segment(ReadOnlyMemory<T> memory)
                => Memory = memory;
            public Segment<T> Add(ReadOnlyMemory<T> mem)
            {
                var segment = new Segment<T>(mem);
                segment.RunningIndex = RunningIndex +
                            Memory.Length;
                Next = segment;
                return segment;
            }
        }

    }

}
