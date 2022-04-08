# V8.Extended

## Table Of Content
- [V8.Extended](#v8.extended)
- [Usage](#usage)
  - [Console](#console)
  - [Intervals](#intervals)
  - [Path](#path)
  - [Filesystem](#filesystem)
  - [TextEncoder / TextDecoder](#textencoder--textdecoder)
- [Changes](#changes)
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

## NuGet

Package NuGet can be found here: [https://www.nuget.org/packages/V8.Extended](https://www.nuget.org/packages/V8.Extended).


# Usage

*V8.Extended* comes with a set of modules you can attach to a running V8 engine to extend its functionality. Each module can be added separately, so you can control which modules to add.

## Console

Implements the basic logging methods of the `console` object, including `trace`, `debug`, `info`, `log`, `warn`, `error` and `assert`. 
By default, will output logs to `System.Console` with different colors per log level.

### Add To Engine

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

Implements `setTimeout`, `setInterval`, `clearTimeout` and `clearInterval`, as standard browsers have.


### Add To Engine

To add the `intervals` module:

```cs
// v8 is a V8ScriptEngine instance.
V8Extended.Intervals _intervals = new();
_intervals.Extend(v8);
```

In addition to attaching the JavaScript module to the V8 engine, you also need to update the timers from C# side. You can do it automatically in a background thread:

```cs
// start timers in background. call _intervals.StopEventsLoop(); to stop
_intervals.StartEventsLoopBackground();
```

Or in the same thread:

```cs
// start timers in current thread. call _intervals.StopEventsLoop(); to stop
_intervals.StartEventsLoop();
```

Or call it manually however you like (recommended to call this every 1 ms, so that timers will not suffer from delays):

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


#### WriteExceptionsToConsole

If true (default) and exception occurs from inside a timeout / interval callback, it will automatically use the `console` object to write error to console before propagating the error.

If `console` is not set, will skip it.

## Path

Implements the nodejs `path` module. 

The following methods / members are implemented:

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


### Add To Engine

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

The following methods / members are implemented:

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

Plus the following non-standard methods:

- getType() -> returns 'file', 'dir' or 'none'.
- isdir()
- isfile()

Note that every `fs` method have three versions to it:

- The regular method (eg `fs.readFile`), which accepts a callback with (err, returnVal) to be called when operation is complete.
- The `Sync` version (eg `fs.readFileSync`), which blocks until completion and return the value directly (will throw exception on errors).
- The `Promise` version (located under `fsPromises`, eg `fsPromises.readFile`), which returns a promise.

To learn more about these methods, please see nodejs docs on the `fs` module.


### Add To Engine

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

#### LockPathRoot()

If called, will permanently lock all operation to be inside the `RootFolder` folder (ie preventing the script from leaving that folder) and will also block the ability to change the root folder. For example, if the JavaScript part tries to write file at "../filename.txt" an exception will be thrown.

This action is irreversible, unless you create and attach a new filesystem module to the v8 engine.

Note: I tested most basic cases but there's a chance I've missed something, so please use carefully and **at your own risk**.


## TextEncoder / TextDecoder

Implements the `TextEncoder` and `TextDecoder` classes, which are currently missing from V8. 

The following methods / members are implemented:

- encode()
- decode()
- encoding

**Note: does not support the `TextDecodeOptions` optional argument.**

### Add To Engine

To add the encoding classes:

```cs
// v8 is a V8ScriptEngine instance.
V8Extended.TextEncoder _enc = new();
_enc.Extend(v8);
```

This will add both `TextEncoder` and `TextDecoder` classes.

### Usage Example

```cs
v8.Execute($"let textBytes = (new TextEncoder()).encode('hello world!');");
```

### Additional Options

#### decodePart()

Additional method in `TextDecoder` that accept additional params, `start` and `end`, to decode just a part of the buffer.

# Changes

## 1.0.1

- Added `TextEncoder` and `TextDecoder` classes.

## 1.0.2

- Fixed filesystem async methods to not throw exception when no callback is provided.
- Fixed filesystem async and promises to be actually async.
- Extended the demo project.
- Updated ClearCode and V8 versions to latest (don't affect package, just for compatibility testing).

# License

V8.Extended is distributed under the permissive MIT license.
