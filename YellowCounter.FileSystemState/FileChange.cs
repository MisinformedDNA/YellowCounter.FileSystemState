// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// From https://github.com/dotnet/corefxlab/blob/master/src/System.IO.FileSystem.Watcher.Polling/System/IO/FileChange.cs

using System.IO;

namespace YellowCounter.FileSystemState
{
    public struct FileChange
    {
        internal FileChange(string directory, string path, WatcherChangeTypes type)
        {
            Directory = directory;
            Name = path;
            ChangeType = type;
        }

        public string Directory { get; }
        public string Name { get; }
        public WatcherChangeTypes ChangeType { get; }
    }
}
