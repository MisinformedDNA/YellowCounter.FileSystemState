// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class FileState
    {
        //[NonSerialized]
        public FileStateFlags Flags;
        public int DirectoryRef;
        public int FilenameRef;
        public DateTimeOffset LastWriteTimeUtc;
        public long Length;
    }

    [Flags]
    public enum FileStateFlags : byte
    {
        None = 0,
        Seen = 1,
        Created = 2,
        Changed = 4,
    }
}
