// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class FileState
    {
        public long Version;  // removal notification are implemented something similar to "mark and sweep". This value is incremented in the mark phase
        public string Directory;
        public string Path;
        public DateTimeOffset LastWriteTimeUtc;
        public long Length;
    }
}
