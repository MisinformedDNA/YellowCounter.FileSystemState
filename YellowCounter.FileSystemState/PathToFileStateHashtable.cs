using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class PathToFileStateHashtable : Dictionary<(string directory, string file), FileState>, ISerializable
    {
        public PathToFileStateHashtable() { }

        public void Add(string directory, string file, FileState value) => Add((directory, file), value);

        public void Remove(string directory, string file) => Remove((directory, file));

        public FileState Get(string directory, string file) => this.GetValueOrDefault((directory, file));

        protected PathToFileStateHashtable(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
