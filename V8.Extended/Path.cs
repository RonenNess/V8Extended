
using System.Runtime.InteropServices;

namespace V8Extended
{
    /// <summary>
    /// Implements the path module for the JS runtime.
    /// </summary>
    public class Path : V8Extender
    {
        /// <summary>
        /// Add 'console' object to a V8 engine.
        /// </summary>
        protected override bool ExtendImpl()
        {
            Engine.AddHostObject("__path__", this);
            Engine.Execute(@"
var path;

(function() {

    // cached const values to reduce calls to c#
    let seperator = null;
    let delimiter = null;

    path = {

        // get sperator character
        get sep() {
            if (!seperator) {
                seperator = String.fromCharCode(__path__.Seperator);
            }
            return seperator;
        },
    
        // get delimiter character
        get delimiter() {
            if (!delimiter) {
                delimiter = String.fromCharCode(__path__.Delimiter);
            }
            return delimiter;
        },
    
        // join path
        join: function() {
            return Array.from(arguments).join(this.sep);
        },

        // normalize path, resolving all .. and similar symbols
        normalize: function(path) {
            return __path__.Normalize(path);
        },

        // return parsed path object
        parse: function(path) {
            return __path__.Parse(path);
        },

        // get basename
        basename: function(path) {
            return __path__.Basename(path);
        },

        // get dirname
        dirname: function(path) {
            return __path__.Dirname(path);
        },

        // get extension name
        extname: function(path) {
            return __path__.Extname(path);
        },

        // format data
        format: function(data) {
            return __path__.Format(data.root || '', data.dir || '', data.filename || '', data.name || '', data.ext || '');
        },

        // is absolute path
        isAbsolute: function(path) {
            return __path__.IsAbsolute(path);
        },

        // resolve path to absolute path
        resolve: function() {
            let path = this.join(...arguments);
            return __path__.Resolve(path);
        }
    }
})();
");
            return true;
        }

        /// <summary>
        /// Get the delimiter character.
        /// </summary>
        public char Delimiter => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        /// <summary>
        /// Get the seperator character.
        /// </summary>
        public char Seperator => System.IO.Path.DirectorySeparatorChar;

        /// <summary>
        /// Normalize path.
        /// </summary>
        public string Normalize(string path)
        {
            return System.IO.Path.GetFullPath(path);
        }

        /// <summary>
        /// Parse path.
        /// </summary>
        public PathParsed Parse(string path)
        {
            return new PathParsed()
            {
                dir = System.IO.Path.GetDirectoryName(path) ?? string.Empty,
                root = System.IO.Path.GetPathRoot(path) ?? string.Empty,
                filename = System.IO.Path.GetFileName(path),
                name = System.IO.Path.GetFileNameWithoutExtension(path),
                ext = System.IO.Path.GetExtension(path),
            };
        }

        /// <summary>
        /// Get filename.
        /// </summary>
        public string Basename(string path)
        {
            return System.IO.Path.GetFileName(path);
        }

        /// <summary>
        /// Get directory name.
        /// </summary>
        public string Dirname(string path)
        {
            return System.IO.Path.GetDirectoryName(path) ?? string.Empty;
        }

        /// <summary>
        /// Get extension name.
        /// </summary>
        public string Extname(string path)
        {
            return System.IO.Path.GetExtension(path);
        }

        /// <summary>
        /// Format parsed path into string.
        /// </summary>
        public string Format(PathParsed data)
        {
            return Format(data.root, data.dir, data.filename, data.name, data.ext);
        }

        /// <summary>
        /// Format parsed path into string.
        /// </summary>
        public string Format(string _root, string _dir, string _filename, string _name, string _ext)
        {
            var dir = _dir.StartsWith(_root ?? String.Empty) ? _dir : System.IO.Path.Join(_root, _dir);
            return System.IO.Path.Join(dir, _filename ?? (_name + _ext ?? String.Empty));
        }

        /// <summary>
        /// Get if root is absolute.
        /// </summary>
        public bool IsAbsolute(string path)
        {
            return System.IO.Path.IsPathRooted(path);
        }

        /// <summary>
        /// Resolve path.
        /// </summary>
        public string Resolve(string path)
        {
            return System.IO.Path.GetFullPath(path);
        }
    }

    /// <summary>
    /// Parsed path components.
    /// </summary>
    public struct PathParsed
    {
        public string dir;
        public string root;
        public string filename; // <-- supposed to be 'base' but reserved word in C#
        public string name;
        public string ext;
    }
}
