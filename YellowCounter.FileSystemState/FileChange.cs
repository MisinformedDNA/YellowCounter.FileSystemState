// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// From https://github.com/dotnet/corefxlab/blob/master/src/System.IO.FileSystem.Watcher.Polling/System/IO/FileChange.cs

using System.IO;

namespace YellowCounter.FileSystemState
{
    public class FileChange
    {
        internal FileChange(string directory, string path, WatcherChangeTypes type)
        {
            Directory = directory;
            Name = path;
            ChangeType = type;
        }
        internal FileChange(string directory, string path, WatcherChangeTypes type, string oldDirectory, string oldName)
        {
            Directory = directory;
            Name = path;
            ChangeType = type;
            OldDirectory = oldDirectory;
            OldName = oldName;
        }

        public string Directory { get; }
        public string Name { get; }
        public string OldDirectory { get; }
        public string OldName { get; }
        public WatcherChangeTypes ChangeType { get; }
    }
}
