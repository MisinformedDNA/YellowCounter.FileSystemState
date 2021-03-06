﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO.Enumeration;

namespace YellowCounter.FileSystemState
{
    internal class FileSystemChangeEnumerator: FileSystemEnumerator<string>
    {
        private FileChangeList _changes = new FileChangeList();
        private string _currentDirectory;
        private FileSystemState _watcher;

        public FileSystemChangeEnumerator(FileSystemState watcher)
            : base(watcher.Path, watcher.EnumerationOptions)
        {
            _watcher = watcher;
        }

        public FileChangeList Changes => _changes;

        protected override void OnDirectoryFinished(ReadOnlySpan<char> directory)
            => _currentDirectory = null;

        protected override string TransformEntry(ref FileSystemEntry entry)
        {
            _watcher.DetermineChange(_currentDirectory, ref _changes, ref entry);

            return null;
        }

        protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
        {
            // Don't want to convert this to string every time
            if (_currentDirectory == null)
                _currentDirectory = entry.Directory.ToString();

            return _watcher.ShouldIncludeEntry(ref entry);
        }

        protected override bool ShouldRecurseIntoEntry(ref FileSystemEntry entry)
        {
            return _watcher.ShouldRecurseIntoEntry(ref entry);
        }
    }
}
