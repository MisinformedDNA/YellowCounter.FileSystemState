using System.Collections.Generic;

namespace YellowCounter.FileSystemState
{
    internal class PathToFileStateHashtable : Dictionary<(string directory, string file), FileState>
    {
        public void Add(string directory, string file, FileState value) => Add((directory, file), value);

        public void Remove(string directory, string file) => Remove((directory, file));

        public FileState Get(string directory, string file) => this.GetValueOrDefault((directory, file));
    }
}
