

namespace V8Extended
{
    /// <summary>
    /// Implement intervals and timeouts for the V8 engine.
    /// </summary>
    public class Intervals : V8Extender
    {
        // to reduce garbage creation from engine.Execute()
        Microsoft.ClearScript.V8.V8Script _compiledScript;

        /// <summary>
        /// If true and 'console' exist, will catch and write exceptions to console.
        /// </summary>
        public bool WriteExceptionsToConsole = true;

        /// <summary>
        /// Optional exception handler to call every time we get an exception while executing timeout or interval.
        /// </summary>
        public Action<Exception> OnException;

        /// <summary>
        /// If true, will pause all intervals and timeouts.
        /// </summary>
        public bool Pause = false;

        /// <summary>
        /// Add 'setTimeout' and 'setInterval' methods to a V8 engine.
        /// </summary>
        protected override bool ExtendImpl()
        {
            // add callbacks dictionary and host object
            Engine.Execute(@"
var __IntervalsAndTimeouts__ = {

    list: [],

    _nextId: 1,

    clear: function()
    {
        this.list = [];
    },

    push: function(data)
    {
        data.id = this._nextId++;
        this.list.push(data);
        this.sort();
        return data.id;
    },

    sort: function() {
        this.list.sort(function(a, b) {
          return a.expires - b.expires;
        });
    },

    remove: function(id)
    {
        for (let i = 0; i < this.list.length; ++i) {
            if (this.list[i].id === id) {
                this.list.splice(i, 1);
                return;
            }
        }
    },
};");

            // add timeout callbacks
            Engine.Execute(@"
var setTimeout = function(cb, interval) {
    if (typeof cb !== 'function') { cb = function() {}; }
    let id = __IntervalsAndTimeouts__.push({callback: cb, expires: new Date().getTime() + interval, interval: interval, repeats: false});
    return id;
};
var clearTimeout = function(id) {
    __IntervalsAndTimeouts__.remove(id)
};
            ");

            // add interval callbacks
            Engine.Execute(@"
var setInterval = function(cb, interval) {
    if (typeof cb !== 'function') { cb = function() {}; }
    let id = __IntervalsAndTimeouts__.push({callback: cb, expires: new Date().getTime() + interval, interval: interval, repeats: true});
    return id;
};
var clearInterval = function(id) {
    __IntervalsAndTimeouts__.remove(id)
};
            ");

            // compile code to execute timeout or interval
            _compiledScript = Engine.Compile(@"
{

    // iterate timers and check what expired
    let _needSort_ = false;
    let _toRemove_ = [];
    for (let curr of __IntervalsAndTimeouts__.list)
    {
        // get timestamp and if we reached something that didn't expire, break
        let currTs = new Date().getTime();
        if (currTs < curr.expires) { break; }
        
        try
        {
            // update repeating invervals
            if (curr.repeats) {
                curr.expires = curr.expires + curr.interval;
                _needSort_ = true;
            }
            // remove if not repeating
            else {
                _toRemove_.push(curr);
            }

            // trigger callback
            curr.callback();
        }
        catch (e)
        {
            if (" + (WriteExceptionsToConsole ? "true" : "false") + @" && typeof console !== 'undefined' && console.error) {
                console.error(e);
            }
            throw e;
        }
    }

    // remove stuff we need to remove
    for (let rem of _toRemove_) {
        __IntervalsAndTimeouts__.remove(rem.id);
    }

    // do sorting if needed
    if (_needSort_) {
        __IntervalsAndTimeouts__.sort();
    }
}");

            return true;
        }

        /// <summary>
        /// Is the event loop currently running?
        /// </summary>
        public bool Running { get; private set; }

        // background thread to run events
        Task _backgroundThread;

        /// <summary>
        /// Clear all timeouts and intervals.
        /// </summary>
        public void ClearAll()
        {
            Engine.Execute("__IntervalsAndTimeouts__.clear();");
        }

        /// <summary>
        /// Start events loop in background task.
        /// </summary>
        public void StartEventsLoopBackground()
        {
            _backgroundThread = Task.Run(StartEventsLoop);
        }

        /// <summary>
        /// Run the events loop in current thread.
        /// </summary>
        public async void StartEventsLoop()
        {
            // stop previous run
            if (Running)
            {
                StopEventsLoop();
            }

            // loop over scheduled events
            Running = true;
            while (Running)
            {
                await Task.Delay(1);
                DoEvents();
            }
        }

        public void StopEventsLoop()
        {
            Running = false;
            if (_backgroundThread != null)
            {
                _backgroundThread.Wait();
                _backgroundThread = null;
            }
        }

        /// <summary>
        /// Execute all expired events manually (you can call this instead of StartEventsLoop / StartEventsLoopBackground).
        /// </summary>
        public void DoEvents()
        {
            try
            {
                Engine.Execute(_compiledScript);
            }
            catch (Exception ex)
            {
                OnException?.Invoke(ex);
            }
        }
    }
}
