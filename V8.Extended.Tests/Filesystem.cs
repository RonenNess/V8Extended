using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;

namespace Tests.V8.Extended
{
    /// <summary>
    /// Test the V8.Extended Console component.
    /// </summary>
    [TestClass]
    public class Filesystem
    {
        // engine and console
        private V8ScriptEngine _v8 = new();
        V8Extended.Filesystem _fs = new();
        TestUtilsContext _utils;

        /// <summary>
        /// Setup tests.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // init filesystem and utils
            _fs.Extend(_v8);
            _utils = TestUtils.InitTestUtils(_v8);

            // create test folder and set as root
            var testFolderName = "test_folder";
            if (System.IO.Directory.Exists(testFolderName))
            {
                System.IO.Directory.Delete(testFolderName, true);
            }
            System.IO.Directory.CreateDirectory(testFolderName);
            _fs.RootFolder = testFolderName;
        }

        /// <summary>
        /// Read text file.
        /// </summary>
        [TestMethod]
        public void ReadFile()
        {
            // read file
            _utils.TestValueStr = null;
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            _v8.Execute($"let data = fs.readFileSync('test.txt'); _testUtils_.TestValueStr = data;");
            Assert.AreEqual("hello world!", _utils.TestValueStr);
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));

            // file not found
            Assert.ThrowsException<ScriptEngineException>(() => { _v8.Execute($"let data2 = fs.readFileSync('not_exist.txt')"); });
        }

        /// <summary>
        /// Write text file.
        /// </summary>
        [TestMethod]
        public void WriteFile()
        {
            _v8.Execute($"fs.writeFileSync('test.txt', 'hello world!');");
            Assert.AreEqual("hello world!", System.IO.File.ReadAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
        }

        /// <summary>
        /// Append text file.
        /// </summary>
        [TestMethod]
        public void AppendFile()
        {
            _v8.Execute($"fs.appendFileSync('test.txt', 'hello'); fs.appendFileSync('test.txt', ' world'); fs.appendFileSync('test.txt', ' 2!');");
            Assert.AreEqual("hello world 2!", System.IO.File.ReadAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
        }

        /// <summary>
        /// Get files with readdir.
        /// </summary>
        [TestMethod]
        public void ReadDir()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test2.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            _v8.Execute($"let readdir = fs.readdirSync(''); _testUtils_.TestValuesStrList = readdir;");
            Assert.AreEqual(
                string.Join(',', (new string[] { "test.txt", "test2.txt", "dirname" }).OrderBy(x => x)),
                string.Join(',', (_utils.TestValuesStrList).OrderBy(x => x)));

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test2.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
        }

        /// <summary>
        /// Check if file or folder exists.
        /// </summary>
        [TestMethod]
        public void Exists()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            _v8.Execute($"_testUtils_.TestValue = fs.existsSync('test.txt');");
            Assert.AreEqual(true, _utils.TestValue);

            _v8.Execute($"_testUtils_.TestValue = fs.existsSync('dirname');");
            Assert.AreEqual(true, _utils.TestValue);

            _v8.Execute($"_testUtils_.TestValue = fs.existsSync('nope.txt');");
            Assert.AreEqual(false, _utils.TestValue);

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
        }

        /// <summary>
        /// Unlink files and folders.
        /// </summary>
        [TestMethod]
        public void Unlink()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            _v8.Execute($"fs.unlinkSync('test.txt');");
            Assert.IsFalse(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));

            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            _v8.Execute($"fs.unlinkSync('dirname');");
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
        }

        /// <summary>
        /// Rename files and folders.
        /// </summary>
        [TestMethod]
        public void Rename()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            _v8.Execute($"fs.renameSync('test.txt', 'test2.txt');");
            Assert.IsFalse(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test2.txt")));

            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            _v8.Execute($"fs.renameSync('dirname', 'dirname2');");
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname2")));

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test2.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname2"));
        }

        /// <summary>
        /// rename files.
        /// </summary>
        [TestMethod]
        public void RenameFile()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute($"fs.renameFileSync('dirname', 'dirname2');"));
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));

            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            _v8.Execute($"fs.renameFileSync('test.txt', 'test2.txt');");
            Assert.IsFalse(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test2.txt")));

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test2.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
        }

        /// <summary>
        /// rename dirs.
        /// </summary>
        [TestMethod]
        public void RenameDirectory()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute($"fs.renameDirSync('test.txt', 'test2.txt');"));
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(_fs.RootFolder, "test.txt")));

            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            _v8.Execute($"fs.renameDirSync('dirname', 'dirname2');");
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname2")));

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname2"));
        }

        /// <summary>
        /// Check file stat, with callback.
        /// We only test one method since they are auto-generated so we don't test all.
        /// </summary>
        [TestMethod]
        public void StatAsync()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            // test stat on file
            _utils.TestValue = null;
            _v8.Execute("fs.stat('test.txt', (err, val) => { _testUtils_.TestValue = val });");
            Thread.Sleep(50);
            Assert.AreNotEqual(null, _utils.TestValue);
            var stats = (V8Extended.FileStat)_utils.TestValue;
            Assert.AreEqual("hello world!".Length, stats.size);
            Assert.AreEqual(".txt", stats.extension);
            Assert.AreEqual("test.txt", stats.name);

            // delete file
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
        }

        /// <summary>
        /// Check file stat, with promise.
        /// We only test one method since they are auto-generated so we don't test all.
        /// </summary>
        [TestMethod]
        public void StatPromise()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            // test stat on file
            _utils.TestValue = null;
            _v8.Execute(@"(async function() {
    _testUtils_.TestValue = await fsPromises.stat('test.txt');
})();");
            Thread.Sleep(250);
            Assert.AreNotEqual(null, _utils.TestValue);
            var stats = (V8Extended.FileStat)_utils.TestValue;
            Assert.AreEqual("hello world!".Length, stats.size);
            Assert.AreEqual(".txt", stats.extension);
            Assert.AreEqual("test.txt", stats.name);

            // delete file
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
        }

        /// <summary>
        /// Check file stat.
        /// </summary>
        [TestMethod]
        public void Stat()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            // stat on non existing file throws
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute($"fs.statSync('not exist');"));

            // test stat on file
            _utils.TestValue = null;
            _v8.Execute($"_testUtils_.TestValue = fs.statSync('test.txt');");
            Assert.AreNotEqual(null, _utils.TestValue);
            var stats = (V8Extended.FileStat)_utils.TestValue;
            Assert.AreEqual("hello world!".Length, stats.size);
            Assert.AreEqual(".txt", stats.extension);
            Assert.AreEqual("test.txt", stats.name);
            Assert.IsTrue(stats.atimeMs > 1647982969434);
            Assert.IsTrue(stats.mtimeMs > 1647982969434);
            Assert.IsTrue(stats.birthtimeMs > 1647982969434);
            Assert.IsTrue(stats.isFile());
            Assert.IsFalse(stats.isDirectory());
            Assert.IsFalse(stats.isReadonly());

            // test stat on dir
            _utils.TestValue = null;
            _v8.Execute($"_testUtils_.TestValue = fs.statSync('dirname');");
            Assert.AreNotEqual(null, _utils.TestValue);
            stats = (V8Extended.FileStat)_utils.TestValue;
            Assert.AreEqual(0, stats.size);
            Assert.AreEqual("", stats.extension);
            Assert.AreEqual("dirname", stats.name);
            Assert.IsTrue(stats.atimeMs > 1647982969434);
            Assert.IsTrue(stats.mtimeMs > 1647982969434);
            Assert.IsTrue(stats.birthtimeMs > 1647982969434);
            Assert.IsTrue(stats.isDirectory());
            Assert.IsFalse(stats.isFile());
            Assert.IsFalse(stats.isReadonly());

            // delete file and dir
            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
        }

        /// <summary>
        /// Test mkdir.
        /// </summary>
        [TestMethod]
        public void MakeDir()
        {
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            _v8.Execute($"fs.mkdirSync('dirname');");
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
        }

        /// <summary>
        /// Test rmdir.
        /// </summary>
        [TestMethod]
        public void RemoveDir()
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname", "internal"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "dirname", "test.txt"), "hello world!");

            // try to delete non-empty folder without recursive flag, and fail.
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute($"fs.rmdirSync('dirname')"));
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));

            // delete directory with stuff in it recursively, using the recursive flag.
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname", "internal")));
            _v8.Execute($"fs.rmdirSync('dirname', {{ recursive: true }});");
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));

            // delete empty directory, without the recursive flag.
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            _v8.Execute($"fs.rmdirSync('dirname');");
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));

            // delete empty directory, with the recursive flag.
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
            Assert.IsTrue(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
            _v8.Execute($"fs.rmdirSync('dirname', {{ recursive: true }});");
            Assert.IsFalse(System.IO.Directory.Exists(System.IO.Path.Combine(_fs.RootFolder, "dirname")));
        }

        /// <summary>
        /// Check GetType(), isfile() and isdir().
        /// </summary>
        [TestMethod]
        public void CheckFileType()
        {
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(_fs.RootFolder, "dirname"));

            _v8.Execute($"_testUtils_.TestValue = fs.getTypeSync('dirname');");
            Assert.AreEqual("dir", _utils.TestValue);

            _v8.Execute($"_testUtils_.TestValue = fs.getTypeSync('test.txt');");
            Assert.AreEqual("file", _utils.TestValue);

            _v8.Execute($"_testUtils_.TestValue = fs.isfileSync('test.txt');");
            Assert.IsTrue((bool)_utils.TestValue);
            _v8.Execute($"_testUtils_.TestValue = fs.isfileSync('dirname');");
            Assert.IsFalse((bool)_utils.TestValue);

            _v8.Execute($"_testUtils_.TestValue = fs.isdirSync('dirname');");
            Assert.IsTrue((bool)_utils.TestValue);
            _v8.Execute($"_testUtils_.TestValue = fs.isdirSync('test.txt');");
            Assert.IsFalse((bool)_utils.TestValue);

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
            System.IO.Directory.Delete(System.IO.Path.Combine(_fs.RootFolder, "dirname"));
        }

        /// <summary>
        /// Lock inside root.
        /// </summary>
        [TestMethod]
        public void CageRoot()
        {
            _fs.LockPathRoot();
            System.IO.File.WriteAllText(System.IO.Path.Combine(_fs.RootFolder, "test.txt"), "hello world!");

            Assert.ThrowsException<InvalidOperationException>(() => _fs.RootFolder = "test");
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute("fs.existsSync('/test.txt')"));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute("fs.existsSync('../test.txt')"));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute("fs.existsSync('..\\\\test.txt')"));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute("fs.existsSync('dir/../../test.txt')"));
            Assert.ThrowsException<ScriptEngineException>(() => _v8.Execute("fs.existsSync('dir\\\\..\\\\..\\\\test.txt')"));

            // just to make sure don't throw exception..
            _v8.Execute($"fs.existsSync('test.txt');");
            _v8.Execute($"fs.existsSync('dir/../test.txt');");
            _v8.Execute($"fs.existsSync('dir\\..\\test.txt');");

            System.IO.File.Delete(System.IO.Path.Combine(_fs.RootFolder, "test.txt"));
        }
    }
}
