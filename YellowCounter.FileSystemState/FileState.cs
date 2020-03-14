// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace YellowCounter.FileSystemState
{
    [Serializable]
    internal class FileState
    {
        [NonSerialized]
        public long LastSeenVersion;  // removal notification are implemented something similar to "mark and sweep". This value is incremented in the mark phase

        [NonSerialized]
        public long CreateVersion;
        [NonSerialized]
        public long ChangeVersion;

        public int DirectoryRef;
        public int FilenameRef;
        public DateTimeOffset LastWriteTimeUtc;
        public long Length;

        internal FileState Clone() => (FileState)this.MemberwiseClone();
    }
}
