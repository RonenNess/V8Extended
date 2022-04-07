using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Linq;

namespace Tests.V8.Extended
{
    /// <summary>
    /// Test the V8.Extended TextEncoder component.
    /// </summary>
    [TestClass]
    public class TextEncoder
    {
        // engine and console
        private V8ScriptEngine _v8 = new();
        V8Extended.TextEncoder _te = new();
        TestUtilsContext _utils;

        /// <summary>
        /// Setup tests.
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            // init text encoder and utils
            _te.Extend(_v8);
            _utils = TestUtils.InitTestUtils(_v8);
        }

        /// <summary>
        /// Test the const values.
        /// </summary>
        [TestMethod]
        public void Consts()
        {
            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = (new TextEncoder()).encoding;");
            Assert.AreEqual("utf-8", _utils.TestValue);

            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = (new TextDecoder()).encoding;");
            Assert.AreEqual("utf-8", _utils.TestValue);
        }

        /// <summary>
        /// Test encoding.
        /// </summary>
        [TestMethod]
        public void Encoding()
        {
            // make sure we return the correct type
            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = (new TextEncoder()).encode('hello').constructor.name;");
            Assert.AreEqual("Uint8Array", _utils.TestValue);

            // test simple ascii encoding
            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = (new TextEncoder()).encode('hello');");
            CollectionAssert.AreEqual(new byte[] {104, 101, 108, 108, 111}, (_utils.TestValue as ITypedArray<byte>).ArrayBuffer.GetBytes());

            // test non ascii encoding
            _utils.TestValue = null;
            _v8.Execute("_testUtils_.TestValue = (new TextEncoder()).encode('hello עולם €');");
            CollectionAssert.AreEqual(new byte[] { 104, 101, 108, 108, 111, 32, 215, 162, 215, 149, 215, 156, 215, 157, 32, 226, 130, 172 }, (_utils.TestValue as ITypedArray<byte>).ArrayBuffer.GetBytes());
        }
    }
}