using Microsoft.ClearScript.V8;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tests.V8.Extended
{
    /// <summary>
    /// Test the V8.Extended Console component.
    /// </summary>
    [TestClass]
    public class Console
    {
        // engine and console
        private V8ScriptEngine _v8 = new();
        V8Extended.Console _console = new();
        TestUtilsContext _utils;

        // store message string + level
        struct MessageAndLevel
        {
            public string Message;
            public V8Extended.ConsoleLevel Level;
        }

        /// <summary>
        /// Setup tests.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // init console and utils
            _console.Extend(_v8);
            _utils = TestUtils.InitTestUtils(_v8);

            // attach custom handler to set test value every time we call console
            _console.AddHandler((string msg, V8Extended.ConsoleLevel level) =>
            {
                var msgAndLevel = new MessageAndLevel() { Message = msg, Level = level };
                _utils.TestValue = msgAndLevel;
                return true;
            });
        }

        /// <summary>
        /// Write all type of console logs (trace, debug, info..).
        /// </summary>
        [TestMethod]
        public void WriteLogs()
        {
            foreach (var level in Enum.GetValues(typeof(V8Extended.ConsoleLevel)))
            {
                if ((V8Extended.ConsoleLevel)level == V8Extended.ConsoleLevel.Assert) { continue; }
                _utils.TestValue = null;
                _v8.Execute($"console.{level.ToString().ToLower()}('hello', 'world', '!');");
                Assert.AreEqual("hello world !", ((MessageAndLevel)_utils.TestValue).Message);
                Assert.AreEqual(level, ((MessageAndLevel)_utils.TestValue).Level);
            }
        }

        /// <summary>
        /// Make sure colors work.
        /// </summary>
        [TestMethod]
        public void Colors()
        {
            foreach (var level in Enum.GetValues(typeof(V8Extended.ConsoleLevel)))
            {
                if ((V8Extended.ConsoleLevel)level == V8Extended.ConsoleLevel.Assert) { continue; }
                _utils.TestValue = null;
                _v8.Execute($"console.{level.ToString().ToLower()}('hello', 'world', '!');");
                Assert.AreEqual("hello world !", ((MessageAndLevel)_utils.TestValue).Message);
                Assert.AreEqual(level, ((MessageAndLevel)_utils.TestValue).Level);
                Assert.AreEqual(_console.LogLevelColor[(V8Extended.ConsoleLevel)level], _console.LastColorSet);
            }
        }

        /// <summary>
        /// Test the console.assert method.
        /// </summary>
        [TestMethod]
        public void AssertLog()
        {
            _utils.TestValue = "not called!";
            _v8.Execute($"console.assert(true, 'hello world!');");
            Assert.AreEqual("not called!", _utils.TestValue);

            _v8.Execute($"console.assert(false, 'hello', 'world!');");
            Assert.AreEqual("hello world!", ((MessageAndLevel)_utils.TestValue).Message);
            Assert.AreEqual(V8Extended.ConsoleLevel.Assert, ((MessageAndLevel)_utils.TestValue).Level);

            _v8.Execute($"console.assert(null, 'hello', 'world', '2!');");
            Assert.AreEqual("hello world 2!", ((MessageAndLevel)_utils.TestValue).Message);
            Assert.AreEqual(V8Extended.ConsoleLevel.Assert, ((MessageAndLevel)_utils.TestValue).Level);
        }
    }
}