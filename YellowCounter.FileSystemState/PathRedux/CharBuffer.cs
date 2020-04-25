using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace YellowCounter.FileSystemState.PathRedux
{
    public class CharBuffer
    {

        private Memory<char> buffer;
        private int pos;

        public CharBuffer(int capacity)
        {
            buffer = new char[capacity];
        }

        public int Capacity => buffer.Length;

        public void Resize(int capacity)
        {
            if(capacity < pos)
                throw new Exception("Cannot resize because data truncation would occur");

            var newBuffer = new char[capacity];

            this.buffer.CopyTo(newBuffer);

            this.buffer = newBuffer;
        }

        public int Store(ReadOnlySpan<char> input)
        {
            // We need space for our text, our null terminator, and an extra
            // null terminator for the end of the buffer.
            if(input.Length + pos + 1 >= buffer.Length)
                return -1;

            var bufSpan = buffer.Span;

            // Return current buffer start position as the result.
            var result = pos;

            // Write the text into our buffer
            input.CopyTo(bufSpan.Slice(pos, input.Length));
            pos += input.Length;

            // Null terminate
            bufSpan[pos] = '\0';
            pos++;

            return result;
        }

        public int Match(ReadOnlySpan<char> arg, ReadOnlySpan<int> indices)
        {
            var bufSpan = buffer.Span;

            foreach(int idx in indices)
            {
                if(bufSpan.Slice(idx, arg.Length).SequenceEqual(arg))
                {
                    // Check for null terminator so we don't match to a
                    // longer string.
                    if(bufSpan[idx + arg.Length] == '\0')
                        return idx;
                }
            }

            // -1 for not found.
            return -1;
        }

        public ReadOnlySpan<char> Retrieve(int index)
        {
            var bufSpan = buffer.Span;

            var begin = bufSpan.Slice(index);

            int len = begin.IndexOf('\0');

            return begin.Slice(0, len);
        }

        public string CreateString(IEnumerable<int> indices)
        {
            int totalLen = 0;
            var posLens = new List<PosLen>();
            // Gather up pos / lens

            var bufSpan = buffer.Span;

            foreach(var idx in indices)
            {
                var tail = bufSpan.Slice(idx);
                var len = tail.IndexOf('\0');

                totalLen += len;
                posLens.Add(new PosLen(idx, len));
                //var text = tail.Slice(0, len);
            }

            // String in REVERSE ORDER of indices - this is because we start at
            // the end and then point back to the parent, grandparent etc.
            return String.Create(totalLen, (buffer, posLens, totalLen),
                (chars, state) =>
                {
                    var span = state.buffer.Span;
                    var pos = state.totalLen;

                    foreach(var posLen in posLens)
                    {
                        var text = span.Slice(posLen.Pos, posLen.Len);

                        pos -= posLen.Len;

                        text.CopyTo(chars.Slice(pos, posLen.Len));
                    }
                });
        }

        private readonly struct PosLen
        {
            public PosLen(int pos, int len)
            {
                this.Pos = pos;
                this.Len = len;
            }
            public int Pos { get; }
            public int Len { get; }
        }

        public Enumerator GetEnumerator()
        {
            var bufSpan = buffer.Span;

            return new Enumerator(bufSpan);
        }

        public ref struct Enumerator
        {
            private int pos;
            private int len;
            ReadOnlySpan<char> bufSpan;
            Item current;

            public Enumerator(ReadOnlySpan<char> bufSpan)
            {
                pos = -1;
                len = 0;
                this.bufSpan = bufSpan;
                current = new Item();
            }

            public readonly Item Current => current;
            public bool MoveNext()
            {
                // Advance past zero terminator and previous string.
                pos += 1 + len;

                var tail = bufSpan.Slice(pos);

                // Reached the end? End enumerating. The end of the buffer
                // has a double null terminator \0\0.
                if(tail[0] == '\0')
                    return false;

                len = tail.IndexOf('\0');

                this.current.Span = tail.Slice(0, len);
                this.current.Pos = pos;

                return true;
            }
        }

        public ref struct Item
        {
            public ReadOnlySpan<char> Span;
            public int Pos;
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
