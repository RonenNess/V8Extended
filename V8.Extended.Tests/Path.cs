using Microsoft.ClearScript.V8;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests.V8.Extended
{
    /// <summary>
    /// Test the V8.Extended Path component.
    /// </summary>
    [TestClass]
    public class Path
    {
        // engine and console
        private V8ScriptEngine _v8 = new();
        V8Extended.Path _path = new();
        TestUtilsContext _utils;

        /// <summary>
        /// Setup tests.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // init path and utils
            _path.Extend(_v8);
            _utils = TestUtils.InitTestUtils(_v8);
        }

        /// <summary>
        /// Test the const values like sep or delimiter.
        /// </summary>
        [TestMethod]
        public void Consts()
        {
            // we do it 3 times because values are cached internally - repeating test caching
            for (var i = 0; i < 3; ++i)
            {
                _utils.TestValue = null;
                _v8.Execute("_testUtils_.TestValue = path.sep;");
                Assert.AreEqual(System.IO.Path.DirectorySeparatorChar.ToString(), _utils.TestValue);

                _utils.TestValue = null;
                _v8.Execute("_testUtils_.TestValue = path.delimiter;");
                Assert.AreEqual(_path.Delimiter.ToString(), _utils.TestValue);
            }
        }

        /// <summary>
        /// Test path join.
        /// </summary>
        [TestMethod]
        public void Join()
        {
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.join('hello', 1, 'world', 'foo.html');");
            Assert.AreEqual(System.IO.Path.Join("hello", "1", "world", "foo.html"), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.join('hello');");
            Assert.AreEqual(System.IO.Path.Join("hello"), _utils.TestValueStr);
        }

        /// <summary>
        /// Test path resolve.
        /// </summary>
        [TestMethod]
        public void Resolve()
        {
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.resolve('hello', 1, 'world', 'foo.html');");
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.Join("hello", "1", "world", "foo.html")), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.resolve('hello');");
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.Join("hello")), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.resolve('hello', 1, 'world', '..', 'foo.html');");
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.Join("hello", "1", "foo.html")), _utils.TestValueStr);
        }

        /// <summary>
        /// Test path basename.
        /// </summary>
        [TestMethod]
        public void Basename()
        {
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.basename(path.join('hello', 1, 'world', 'foo.html'));");
            Assert.AreEqual(System.IO.Path.Join("foo.html"), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.basename(path.join('hello', 1, 'world'));");
            Assert.AreEqual(System.IO.Path.Join("world"), _utils.TestValueStr);
        }

        /// <summary>
        /// Test path dirname.
        /// </summary>
        [TestMethod]
        public void Dirname()
        {
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.dirname(path.join('hello', 1, 'world', 'foo.html'));");
            Assert.AreEqual(System.IO.Path.Join("hello", "1", "world"), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.dirname(path.join('hello', 1, 'world'));");
            Assert.AreEqual(System.IO.Path.Join("hello", "1"), _utils.TestValueStr);
        }

        /// <summary>
        /// Test path extname.
        /// </summary>
        [TestMethod]
        public void Extname()
        {
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.extname(path.join('hello', 1, 'world', 'foo.html'));");
            Assert.AreEqual(".html", _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.extname('a.b');");
            Assert.AreEqual(".b", _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.extname(path.join('hello', 1, 'world'));");
            Assert.AreEqual("", _utils.TestValueStr);
        }

        /// <summary>
        /// Test path normalize.
        /// </summary>
        [TestMethod]
        public void Normalize()
        {
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.normalize(path.join('hello', 1, 'world', 'foo.html'));");
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.Join("hello", "1", "world", "foo.html")), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.normalize(path.join('hello'));");
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.Join("hello")), _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValueStr = path.normalize(path.join('..', 'hello'));");
            Assert.AreEqual(System.IO.Path.GetFullPath(System.IO.Path.Join("..", "hello")), _utils.TestValueStr);
        }

        /// <summary>
        /// Test path isAbsolute.
        /// </summary>
        [TestMethod]
        public void IsAbsolute()
        {
            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('');");
            Assert.AreEqual(false, _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('hello');");
            Assert.AreEqual(false, _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('hello\\\\world');");
            Assert.AreEqual(false, _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('hello\\\\..\\\\world');");
            Assert.AreEqual(false, _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('\\\\hello\\\\world');");
            Assert.AreEqual(true, _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('c:\\\\hello\\\\world');");
            Assert.AreEqual(true, _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.isAbsolute('/hello\\\\world');");
            Assert.AreEqual(true, _utils.TestValue);
        }

        /// <summary>
        /// Test path parse.
        /// </summary>
        [TestMethod]
        public void Parse()
        {
            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.parse('c:\\\\test\\\\foo\\\\file.txt');");
            var data = (V8Extended.PathParsed)_utils.TestValue;
            Assert.AreEqual(".txt", data.ext);
            Assert.AreEqual("file", data.name);
            Assert.AreEqual("file.txt", data.filename);
            Assert.AreEqual("c:\\test\\foo", data.dir);
            Assert.AreEqual("c:\\", data.root);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.parse('test\\\\foo');");
            data = (V8Extended.PathParsed)_utils.TestValue;
            Assert.AreEqual("", data.ext);
            Assert.AreEqual("foo", data.name);
            Assert.AreEqual("foo", data.filename);
            Assert.AreEqual("test", data.dir);
            Assert.AreEqual("", data.root);
        }

        /// <summary>
        /// Test path format.
        /// </summary>
        [TestMethod]
        public void Format()
        {
            _utils.TestValue = null;
            _utils.TestValueStr = null;
            _v8.Execute("_testUtils_.TestValue = path.parse('c:\\\\test\\\\foo\\\\file.txt');");
            var data = (V8Extended.PathParsed)_utils.TestValue;
            Assert.AreEqual(".txt", data.ext);
            Assert.AreEqual("file", data.name);
            Assert.AreEqual("file.txt", data.filename);
            Assert.AreEqual("c:\\test\\foo", data.dir);
            Assert.AreEqual("c:\\", data.root);
            _v8.Execute("_testUtils_.TestValueStr = path.format(_testUtils_.TestValue);");
            Assert.AreEqual("c:\\test\\foo\\file.txt", _utils.TestValueStr);

            _utils.TestValueStr = null;
            _v8.Execute("var _pdata = {root: 'c:\\\\', dir: 'test\\\\foo', filename: 'file.txt'}; _testUtils_.TestValueStr = path.format(_pdata);");
            Assert.AreEqual("c:\\test\\foo\\file.txt", _utils.TestValueStr);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = path.parse('test\\\\foo');");
            data = (V8Extended.PathParsed)_utils.TestValue;
            Assert.AreEqual("", data.ext);
            Assert.AreEqual("foo", data.name);
            Assert.AreEqual("foo", data.filename);
            Assert.AreEqual("test", data.dir);
            Assert.AreEqual("", data.root);
            _v8.Execute("_testUtils_.TestValueStr = path.format(_testUtils_.TestValue);");
            Assert.AreEqual("test\\foo", _utils.TestValueStr);
        }
    }
}