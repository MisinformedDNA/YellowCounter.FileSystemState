using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using YellowCounter.FileSystemState;
using Shouldly;

namespace PathReduxTests.Watcher
{
    [TestClass]
    public class WatcherTests
    {
        [TestMethod]
        public void FileSystemWatcherNoChange()
        {
            var dir = GetRandomDirectory();

            try
            {

                File.WriteAllText(Path.Combine(dir, "text1.txt"), "Hello");
                File.WriteAllText(Path.Combine(dir, "blah.txt"), "Hello");

                var watcher = new FileSystemState(dir, options: new EnumerationOptions { RecurseSubdirectories = true });
                watcher.LoadState();

                var q = watcher.GetChanges();
                q.Count.ShouldBe(0);

            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        private string GetRandomDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
