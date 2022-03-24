
namespace V8Extended
{
    /// <summary>
    /// Implements the console object for the JS runtime.
    /// </summary>
    public class Console : V8Extender
    {
        // custom handlers registered
        List<Func<string, ConsoleLevel, bool>> _handlers = new();

        /// <summary>
        /// If true, will set console colors based on console log level.
        /// </summary>
        public bool UseColors = true;

        /// <summary>
        /// Which console color to use for every console log level.
        /// </summary>
        public Dictionary<ConsoleLevel, ConsoleColor> LogLevelColor = new();

        /// <summary>
        /// Last console color we set.
        /// </summary>
        public ConsoleColor LastColorSet;

        /// <summary>
        /// Create the console implementor for js.
        /// </summary>
        public Console()
        {
            LogLevelColor[ConsoleLevel.Info] = ConsoleColor.White;
            LogLevelColor[ConsoleLevel.Log] = ConsoleColor.Gray;
            LogLevelColor[ConsoleLevel.Error] = ConsoleColor.Red;
            LogLevelColor[ConsoleLevel.Warn] = ConsoleColor.Yellow;
            LogLevelColor[ConsoleLevel.Assert] = ConsoleColor.DarkRed;
            LogLevelColor[ConsoleLevel.Debug] = ConsoleColor.Blue;
            LogLevelColor[ConsoleLevel.Trace] = ConsoleColor.Green;
        }

        /// <summary>
        /// Add 'console' object to a V8 engine.
        /// </summary>
        protected override bool ExtendImpl()
        {
            Engine.AddHostObject("__console__", this);
            Engine.Execute(@"
var console = {
    assert: function(condition, data)
    {
        if (!condition) {
            let msg = Array.from(arguments);
            msg.shift();
            __console__.Assert(msg.join(' '));
            __hostExceptionsHandler__.checkAndThrow();
        }
    },

    trace: function(data)
    {
        __console__.Trace(Array.from(arguments).join(' '));
        __hostExceptionsHandler__.checkAndThrow();
    },

    debug: function(data)
    {
        __console__.Debug(Array.from(arguments).join(' '));
        __hostExceptionsHandler__.checkAndThrow();
    },

    info: function(data)
    {
        __console__.Info(Array.from(arguments).join(' '));
        __hostExceptionsHandler__.checkAndThrow();
    },

    log: function(data)
    {
        __console__.Log(Array.from(arguments).join(' '));
        __hostExceptionsHandler__.checkAndThrow();
    },

    warn: function(data)
    {
        __console__.Warn(Array.from(arguments).join(' '));
        __hostExceptionsHandler__.checkAndThrow();
    },

    error: function(data)
    {
        __console__.Error(Array.from(arguments).join(' '));
        __hostExceptionsHandler__.checkAndThrow();
    },
}
");
            return true;
        }

        /// <summary>
        /// Add custom handler to handle console prints.
        /// </summary>
        /// <param name="callback">Handler method. Return false to break handlers chain.</param>
        public void AddHandler(Func<string, ConsoleLevel, bool> callback)
        {
            _handlers.Add(callback);
        }

        /// <summary>
        /// Implement the way we concat the console print params into a single message to print.
        /// Override this to change the default behavior (concat via space character, ignore level).
        /// </summary>
        public Func<string[], ConsoleLevel, string> FullMessageBuilder = (string[] data, ConsoleLevel level) =>
        {
            return string.Join(" ", data);
        };

        /// <summary>
        /// Write message to console.
        /// </summary>
        /// <param name="level">Console level.</param>
        /// <param name="msg">Message to write.</param>
        protected virtual void WriteMsg(ConsoleLevel level, params string[] msg)
        {
            try
            {
                // concat message string
                var fullMsg = FullMessageBuilder(msg, level);

                // set console colors
                if (UseColors)
                {
                    if (LogLevelColor.TryGetValue(level, out ConsoleColor color))
                    {
                        System.Console.ForegroundColor = LastColorSet = color;
                    }
                    else
                    {
                        System.Console.ResetColor();
                        LastColorSet = System.Console.ForegroundColor;
                    }
                }

                // call handlers and break if one of them return false
                foreach (var handler in _handlers)
                {
                    if (!handler(fullMsg, level))
                    {
                        return;
                    }
                }

                // write console log
                System.Console.WriteLine(fullMsg);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Write assert level message.
        /// Note: the assert condition is tested in JS side.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Assert(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Assert, msg);
        }

        /// <summary>
        /// Write debug level message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Debug(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Debug, msg);
        }

        /// <summary>
        /// Write log level message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Log(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Log, msg);
        }

        /// <summary>
        /// Write info level message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Info(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Info, msg);
        }

        /// <summary>
        /// Write warn level message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Warn(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Warn, msg);
        }

        /// <summary>
        /// Write error level message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Error(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Error, msg);
        }

        /// <summary>
        /// Write trace level message.
        /// </summary>
        /// <param name="msg">Message to write.</param>
        public void Trace(params string[] msg)
        {
            WriteMsg(ConsoleLevel.Trace, msg);
        }
    }

    /// <summary>
    /// Console messages level.
    /// </summary>
    public enum ConsoleLevel
    {
        Trace,
        Debug,
        Info,
        Log, 
        Warn,
        Error,
        Assert
    }
}
