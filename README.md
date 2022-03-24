# V8.Extended

## Table Of Content
- [V8.Extended](#v8.extended)
- [Usage](#usage)
  - [Console](#console)
  - [Intervals](#intervals)
  - [Path](#path)
  - [Filesystem](#filesystem)
- [License](#license)

## About

*V8.Extended* is a small package to add useful common utilities to a *ClearScript.V8 JavaScript Engine*. This include things like `console`, `setTimeout`, `path`, `fs`, etc.

All host objects imitate nodejs / browsers standard behavior as closely as possible, but not 100% of APIs are implemented. 

In addition to the JavaScript-side API, *V8.Extended* provides a host-side (C#) API to fine-tune and control some of the APIs behavior. For example, you can look the `fs` module to a specific folder, or modify `console` colors.

**With V8.Extended enabled, the following code would work**:

```js
console.log("Will write file after one second..");
setTimeout(function() {
    fs.writeFileSync(path.join('subdir', 'hello.txt'), 'hello world!');
    console.log("File saved!");
}, 1000);
```

To see more code examples check out `V8.Extended.Demo`, or look at the tests code at `V8.Extended.Tests`.

# Usage

*V8.Extended* comes with a set of modules you can attach to a running V8 engine to extend its functionality. Each module can be added separately, so you can control which modules to add.

## Console

Implements the basic logging methods of the `console` object, including `trace`, `debug`, `info`, `log`, `warn`, `error` and `assert`. 
By default, will output logs to `System.Console` with different colors per log level.

### Adding Module

To add the `console` module:

```cs
// v8 is a V8ScriptEngine instance.
V8Extended.Console _console = new();
_console.Extend(v8);
```

### Usage Example

```cs
v8.Execute("console.debug('hello', 'world!');");
```

### Additional Options

#### Colors

You can enable / disable console colors:

```cs
// enable colors (default to true)
_console.UseColors = true;
```

And set which color to use per log level:

```cs
// set error level logs to red
_console.LogLevelColor[V8Extended.ConsoleLevel.Error] = ConsoleColor.Red;
```

#### Custom Handlers

You can add custom handlers to handle `console` log calls:

```cs
// add a custom handler to copy all console output to an object called `myLogger`.
// return true to continue to next handler, or false to break the chain (will also skip the default `System.Console` part).
_console.AddHandler((string msg, V8Extended.ConsoleLevel level) =>
{
    myLogger.Write(msg);
    return true;
});
```


## Intervals

Implements `setTimeout`, `setInterval`, `clearTimeout` and `clearInterval` all standard browsers have.

This module is a bit tricky, as it introduce threads into a JavaScript engine that is meant to be single threaded. However, if you're careful enough, it works great!

### Adding Module

To add the `intervals` module:

```cs
// v8 is a V8ScriptEngine instance.
V8Extended.Intervals _intervals = new();
_intervals.Extend(v8);
```

In addition to attaching the JavaScript APIs to the V8 engine, you also need to check the fake events loop used for the timers from C# side. You can do it automatically in a background thread:

```cs
// start timers in background. call _intervals.StopEventsLoop(); to stop
_intervals.StartEventsLoopBackground();
```

Do it in the same thread:

```cs
// start timers in current thread. call _intervals.StopEventsLoop(); to stop
_intervals.StartEventsLoop();
```

Or call it manually however you like (call this often enough, recommended every 1 ms, so that timers will not suffer from delays):

```cs
_intervals.DoEvents();
```

### Usage Example

```cs
v8.Execute("setTimeout(function() { console.debug('1000 ms passed!'); }, 1000);");
```

### Additional Options

#### Pause

Temporarily pause the events loop.

#### ClearAll()

Clear all timeouts and intervals.

#### OnException

Optional errors handler you can attach to handle exceptions from inside the timers execution.


*V8.Extended* comes with a set of modules you can attach to a running V8 engine to extend its functionality. Each module can be added separately, so you can control which modules to add.

#### WriteExceptionsToConsole

If true (default) and exception occurs from inside a timeout / interval callback, will use the `console` object to write error to console before proceeding.

If `console` is not set, will skip it.

## Path

Implements the nodejs `path` module. 

The following methods / fields are implemented:

- sep
- delimiter
- join()
- normalize()
- parse()
- basename()
- dirname()
- extname()
- format()
- isAbsolute()
- resolve()

To learn more about these methods, please see nodejs docs on `Path` module.


### Adding Module

To add the `path` module:

```cs
// v8 is a V8ScriptEngine instance.
V8Extended.Path _path = new();
_path.Extend(v8);
```

### Usage Example

```cs
v8.Execute("var filePath = path.join('hello', 'world!');");
```

## Filesystem

Implements the nodejs `fs` module. 

The following methods / fields are implemented:

- readFile()
- writeFile()
- appendFile()
- readdir()
- exists()
- unlink()
- rename()
- renameFile()
- renameDir()
- mkdir()
- rmdir()
- stat()

Plust the following non-standard methods:

- getType()
- isdir()
- isfile()

Note that every `fs` method have three versions:

- The regular method (eg `fs.readFile`), that accept a callback with (err, returnVal) to invoke when operation is done / fails.
- The `Sync` version (eg `fs.readFileSync`), that blocks until complete and return the value regularly (will throw exception on issues).
- The `Promise` version (located under `fsPromises`, eg `fsPromises.readFile`), that return a promise abd work in background.

To learn more about these methods, please see nodejs docs on the `fs` module.


### Adding Module

To add the `fs` module:

```cs
// v8 is a V8ScriptEngine instance.
V8Extended.Filesystem _fs = new();
_fs.Extend(v8);
```

### Usage Example

```cs
v8.Execute($"fs.writeFileSync('test.txt', 'hello world!');");
```

### Additional Options

#### RootFolder

Set the root folder all filesystem-related operation will be relative to.

#### LockPathToRoot()

If called, will permanently lock all operation to be inside the `RootFolder` folder (preventing user from escaping that folder) and will prevent the ability to change the root folder.

This action is irreversible, unless you create and attach a new filesystem module to the v8 engine.

Note: I can't promise there are no ways to break from this cage, so please use carefully and **at your own risk**.

# License

V8.Extended is distributed under the permissive MIT license.