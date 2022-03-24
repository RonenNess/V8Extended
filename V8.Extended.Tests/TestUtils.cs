using Microsoft.ClearScript.V8;
using System.Collections.Generic;


namespace Tests.V8.Extended
{
    /// <summary>
    /// Test utils and extensions.
    /// </summary>
    internal static class TestUtils
    {
        /// <summary>
        /// Add test utils and methods to an engine instance.
        /// </summary>
        /// <param name="engine"></param>
        public static TestUtilsContext InitTestUtils(V8ScriptEngine engine)
        {
            var ret = new TestUtilsContext();
            engine.AddHostObject("_testUtils_", ret);
            return ret;
        }
    }

    /// <summary>
    /// Returned object from adding test utils to a V8 engine.
    /// Contains the C#-side of the utils.
    /// </summary>
    public class TestUtilsContext
    {
        /// <summary>
        /// Values list we can set from JS side to check in tests.
        /// </summary>
        public string[] TestValuesStrList { get; set; }

        /// <summary>
        /// Value we can set from JS side to check in tests.
        /// </summary>
        public object TestValue { get; set; }

        /// <summary>
        /// Value we can set from JS side to check in tests.
        /// </summary>
        public string TestValueStr { get; set; }
    }
}
