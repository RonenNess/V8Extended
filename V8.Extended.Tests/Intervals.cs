using Microsoft.ClearScript.V8;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Tests.V8.Extended
{
    /// <summary>
    /// Test the V8.Extended Console component.
    /// </summary>
    [TestClass]
    public class Intervals
    {
        // engine and console
        private V8ScriptEngine _v8 = new();
        V8Extended.Intervals _intervals = new();
        TestUtilsContext _utils;

        /// <summary>
        /// Setup tests.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // init intervals and utils
            _intervals.Extend(_v8);
            _utils = TestUtils.InitTestUtils(_v8);
            new V8Extended.Console().Extend(_v8);

            // handle exceptions
            _intervals.OnException = (System.Exception e) =>
            {
                throw new System.Exception("Error while running timeout or interval script!");
            };

            // start intervals events loop
            _intervals.StartEventsLoopBackground();
        }

        /// <summary>
        /// Test timeouts.
        /// </summary>
        [TestMethod]
        public void SetTimeout()
        {
            // set value to null, and set timeout to turn it to 'hello' after 50 ms
            _utils.TestValueStr = null;
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr = 'hello'; }, 50);");

            // make sure not 'hello' yet
            Assert.AreEqual(null, _utils.TestValueStr);

            // after 30 ms make sure its still null
            Thread.Sleep(30);
            Assert.AreEqual(null, _utils.TestValueStr);

            // after 70 ms (30 from before + 40) make sure its hello
            Thread.Sleep(40);
            Assert.AreEqual("hello", _utils.TestValueStr);

            // make sure the timeout happens once
            _utils.TestValueStr = null;
            Thread.Sleep(100);
            Assert.AreEqual(null, _utils.TestValueStr);

            // set value to empty string, and set 3 timeouts to run next iteration
            _utils.TestValueStr = "";
            _intervals.Pause = true;
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr += 'a'; }, 0);");
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr += 'b'; }, 0);");
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr += 'c'; }, 0);");

            // after 100 ms make sure value is 'abc' ie that all timeouts ran by order
            _intervals.Pause = false;
            Thread.Sleep(100);
            Assert.AreEqual("abc", _utils.TestValueStr);

            // set value to empty string, and set 3 timeouts to run at different times
            _utils.TestValueStr = "";
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr += 'b'; }, 50);");
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr += 'a'; }, 10);");
            _v8.Execute("setTimeout(() => { _testUtils_.TestValueStr += 'c'; }, 70);");

            // after 150 ms make sure value is still 'abc'
            Thread.Sleep(150);
            Assert.AreEqual("abc", _utils.TestValueStr);

            // clear all
            _intervals.ClearAll();

            // set timeout from inside timeout
            _intervals.Pause = true;
            _utils.TestValueStr = "";
            _v8.Execute(@"
    let _method_ = () => { _testUtils_.TestValueStr += 'a'; setTimeout(_method_, 50); }
    _method_();
");

            // after 250 ms make sure value is 'aaaaa'
            _intervals.Pause = false;
            Thread.Sleep(260);
            Assert.IsTrue(_utils.TestValueStr == "aaaa" || _utils.TestValueStr == "aaaaa" || _utils.TestValueStr == "aaaaaa");

            // clear all
            _intervals.ClearAll();
        }

        /// <summary>
        /// Test intervals.
        /// </summary>
        [TestMethod]
        public void SetInterval()
        {
            // set value to "", and set interval to add number every 50 ms
            _utils.TestValueStr = "";
            _v8.Execute("var _intCount = 1; let intervalId = setInterval(() => { _testUtils_.TestValueStr += _intCount; _intCount++; }, 50);");

            // make sure still empty
            Assert.AreEqual("", _utils.TestValueStr);

            // after 30 ms make sure its still empty
            Thread.Sleep(30);
            Assert.AreEqual("", _utils.TestValueStr);

            // after 170 ms (30 from before + 140) make sure its 123
            Thread.Sleep(140);
            Assert.AreEqual("123", _utils.TestValueStr);

            // cancel interval
            _v8.Execute("clearInterval(intervalId);");

            // set value to empty string, and set 3 intervals to run next iteration
            _utils.TestValueStr = "";
            _intervals.Pause = true;
            _v8.Execute("let intervalId1 = setInterval(() => { _testUtils_.TestValueStr += 'a'; }, 60);");
            _v8.Execute("let intervalId2 = setInterval(() => { _testUtils_.TestValueStr += 'b'; }, 70);");
            _v8.Execute("let intervalId3 = setInterval(() => { _testUtils_.TestValueStr += 'c'; }, 80);");

            // after 100 ms make sure value is 'abc' ie that all intervals ran by order
            _intervals.Pause = false;
            Thread.Sleep(110);
            Assert.AreEqual("abc", _utils.TestValueStr);

            // after another 100 ms make sure value is at least 'abcabc'
            Thread.Sleep(100);
            Assert.IsTrue(_utils.TestValueStr.StartsWith("abcabc"));

            // clear all
            _intervals.ClearAll();
        }

        /// <summary>
        /// Test clear timeouts.
        /// </summary>
        [TestMethod]
        public void ClearTimeout()
        {
            // set value to null, and set timeout to turn it to 'hello' after 50 ms
            _utils.TestValueStr = null;
            _v8.Execute("let timeoutId = setTimeout(() => { _testUtils_.TestValueStr = 'hello'; }, 50);");

            // make sure not 'hello' yet
            Assert.AreEqual(null, _utils.TestValueStr);

            // cancel timeout
            _v8.Execute("clearTimeout(timeoutId);");

            // after 100 ms make sure its still null
            Thread.Sleep(100);
            Assert.AreEqual(null, _utils.TestValueStr);

            // make sure calling again won't crash, even though timeout doesn't exist
            _v8.Execute("clearTimeout(timeoutId);");

            // clear all
            _intervals.ClearAll();
        }

        /// <summary>
        /// Test clear intervals.
        /// </summary>
        [TestMethod]
        public void ClearInterval()
        {
            // set value to null, and set timeout to turn it to 'hello' after 50 ms
            _utils.TestValueStr = null;
            _v8.Execute("let interId = setInterval(() => { _testUtils_.TestValueStr = 'hello'; }, 50);");

            // make sure not 'hello' yet
            Assert.AreEqual(null, _utils.TestValueStr);

            // cancel timeout
            _v8.Execute("clearInterval(interId);");

            // after 100 ms make sure its still null
            Thread.Sleep(100);
            Assert.AreEqual(null, _utils.TestValueStr);

            // make sure calling again won't crash, even though interval doesn't exist
            _v8.Execute("clearInterval(interId);");

            // clear all
            _intervals.ClearAll();
        }
    }
}
