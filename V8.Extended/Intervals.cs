

namespace V8Extended
{
    /// <summary>
    /// Implement intervals and timeouts for the V8 engine.
    /// </summary>
    public class Intervals : V8Extender
    {
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

    _needSort: false,

    usedIds: new Set(),

    clear: function()
    {
        this.list = [];
        this.usedIds = new Set();
        this._needSort = false;
    },

    push: function(data)
    {
        while (this.usedIds.has(this._nextId)) {
            this._nextId++;
            if (this._nextId >= Number.MAX_SAFE_INTEGER) { this._nextId = 1; }
        }
        this.usedIds.add(this._nextId);
        data.id = this._nextId;
        this.list.push(data);
        this._needSort = true;
        return data.id;
    },

    sortIfNeeded: function() {
        if (this._needSort && this.list.length > 1) {
            this.sort();
        }
    },

    sort: function() {
        this.list.sort(function(a, b) {
          return a.expires - b.expires;
        });
        this._needSort = false;
    },

    remove: function(id)
    {
        for (let i = 0; i < this.list.length; ++i) {
            if (this.list[i].id === id) {
                this.list.splice(i, 1);
                this.usedIds.delete(id);
                return;
            }
        }
    },
};


// method to run intervals.
// this will be invoked from host side.
__IntervalsAndTimeouts__.__doEvents = function() {

    // sort if needed
    this.sortIfNeeded();

    // iterate timers and check what expired
    let _intervals = this.list.slice(0);
    for (let curr of _intervals)
    {
        // get timestamp and if we reached something that didn't expire, break (keep in mind its sorted)
        let currTs = (new Date()).getTime();
        if (currTs+1 < curr.expires) { break; }
        
        // execute current interval / timeout
        try
        {
            // update repeating invervals
            if (curr.repeats) {
                curr.expires = curr.expires + curr.interval;
                this._needSort = true;
            }
            // if its not a repeating interval, remove it
            else {
                this.remove(curr.id);
            }

            // trigger the callback
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
}


// method to create timeouts
var setTimeout = function(cb, interval) {
    if (typeof cb !== 'function') { cb = function() {}; }
    interval = Math.round(interval);
    let id = __IntervalsAndTimeouts__.push({callback: cb, expires: (new Date()).getTime() + interval, interval: interval, repeats: false});
    return id;
};
var clearTimeout = function(id) {
    __IntervalsAndTimeouts__.remove(id)
};


// method to create intervals
var setInterval = function(cb, interval) {
    if (typeof cb !== 'function') { cb = function() {}; }
    interval = Math.round(interval);
    let id = __IntervalsAndTimeouts__.push({callback: cb, expires: (new Date()).getTime() + interval, interval: interval, repeats: true});
    return id;
};
var clearInterval = function(id) {
    __IntervalsAndTimeouts__.remove(id)
};

");

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
            Engine.Script.__IntervalsAndTimeouts__.clear();
        }

        /// <summary>
        /// Start events loop in background task.
        /// </summary>
        /// <remarks>To avoid high CPU usage, this method use `SpinOnce` after every call. This means that timers will only be called every ~15ms.</remarks>
        public void StartEventsLoopBackground()
        {
            _backgroundThread = Task.Run(StartEventsLoop);
        }

        /// <summary>
        /// Run the events loop in current thread in endless loop.
        /// </summary>
        /// <remarks>To avoid high CPU usage, this method use `SpinOnce` after every call. This means that timers will only be called every ~15ms.</remarks>
        public void StartEventsLoop()
        {
            // stop previous run
            if (Running)
            {
                StopEventsLoop();
            }

            // loop over scheduled events
            Running = true;
            SpinWait sw = new SpinWait();
            while (Running)
            {
                DoEvents();
                sw.SpinOnce();
            }
        }

        /// <summary>
        /// Stop running events loop, if StartEventsLoop() or StartEventsLoopBackground() was previously called.
        /// </summary>
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
        /// Execute all expired timeouts / intervals once.
        /// </summary>
        /// <remarks>Instead of calling this manually, you can call StartEventsLoop() or StartEventsLoopBackground() to run in background automatically.</remarks>
        public void DoEvents()
        {
            try
            {
                Engine.Script.__IntervalsAndTimeouts__.__doEvents();
            }
            catch (Exception ex)
            {
                OnException?.Invoke(ex);
            }
        }
    }
}
