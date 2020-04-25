// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Runtime.InteropServices;

namespace YellowCounter.FileSystemState
{
    internal class FileSystemChangeEnumerator : FileSystemEnumerator<object>
    {
        private readonly string filter;
        private IAcceptFileSystemEntry acceptFileSystemEntry;
        //private string currentDirectory;

        private static bool ignoreCase;

        static FileSystemChangeEnumerator()
        {
            ignoreCase = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        public FileSystemChangeEnumerator(
            string filter,
            string path,
            EnumerationOptions enumerationOptions,
            IAcceptFileSystemEntry acceptFileSystemEntry)
            : base(path, enumerationOptions)
        {
            this.filter = filter;
            this.acceptFileSystemEntry = acceptFileSystemEntry;
        }

        public void Scan()
        {
            // Enumerating causes TransformEntry() to be called repeatedly
            while(MoveNext()) { }
        }

        protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
        {
            //currentDirectory = null;

            base.OnDirectoryFinished(directory);
        }

        protected override object TransformEntry(ref FileSystemEntry entry)
        {
            acceptFileSystemEntry.Accept(ref entry);

            return null;
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            //if(currentDirectory == null)
            //    currentDirectory = entry.Directory.ToString();

            if(entry.IsDirectory)
                return false;

            if(FileSystemName.MatchesSimpleExpression(filter, entry.FileName, ignoreCase: ignoreCase))
                return true;

            return false;
        }

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry) => true;
    }
}
