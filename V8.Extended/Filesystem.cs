
namespace V8Extended
{
    /// <summary>
    /// Implements the filesystem module for the JS runtime.
    /// </summary>
    public class Filesystem : V8Extender
    {
        // If true, will not allow users to exit the working folder of the filesystem.
        // The working folder is either defined by 'Root', or the current working directory.
        bool _cagePath = false;

        /// <summary>
        /// Root folder. All IO actions path will be relative to this.
        /// </summary>
        public string RootFolder
        {
            get => _root;
            set 
            { 
                if (_cagePath) { throw new InvalidOperationException("Can't change filesystem root after its been locked!"); }
                _root = value; 
                _rootFull = System.IO.Path.GetFullPath(value); 
            }
        }

        // root folder value.
        string _root = "";
        string _rootFull = "";

        /// <summary>
        /// Create the fs implementor for js.
        /// </summary>
        public Filesystem()
        {
            RootFolder = Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Add 'fs' module to a V8 engine.
        /// </summary>
        protected override bool ExtendImpl()
        {
            Engine.AddHostObject("__Filesystem__", this);
            Engine.Execute(@"
var fs = 
{
    // read all data as text
    readFile: function(path)
    {
        return __Filesystem__.ReadAllText(path);
    },

    // write all data as text
    writeFile: function(path, data)
    {
        __Filesystem__.WriteAllText(path, data);
    },

    // append all data as text
    appendFile: function(path, data)
    {
        __Filesystem__.AppendAllText(path, data);
    },

    // get filenames in dir
    readdir: function(path)
    {
        return __Filesystem__.GetFiles(path);
    },

    // check if a file exists
    exists: function(path)
    {
        return __Filesystem__.Exists(path);
    },

    // delete a file
    unlink: function(path)
    {
        __Filesystem__.Delete(path);
    },

    // rename a file or folder
    rename: function(path, toPath)
    {
        __Filesystem__.Move(path, toPath);
    },

    // rename a file
    renameFile: function(path, toPath)
    {
        __Filesystem__.MoveFile(path, toPath);
    },

    // rename a directory
    renameDir: function(path, toPath)
    {
        __Filesystem__.MoveDirectory(path, toPath);
    },

    // create a folder
    mkdir: function(path)
    {
        __Filesystem__.CreateDirectory(path);
    },

    // delete a folder
    rmdir: function(path, options) 
    {
        __Filesystem__.DeleteDirectory(path, Boolean(options && options.recursive));
    },

    // get file or folder type, returning one of: 'file', 'dir', 'none'.
    getType: function(path)
    {
        return __Filesystem__.GetType(path);
    },

    // check if path is a folder
    isdir: function(path)
    {
        return this.getTypeSync(path) === 'dir';
    },

    // check if path is a file
    isfile: function(path)
    {
        return this.getTypeSync(path) === 'file';
    },

    // get file stat
    stat: function(path)
    {
        return __Filesystem__.Stat(path);
    },
}

// will be later filled with the 'promises' versions of the api
var fsPromises = {};
");

            // build the 'promises' and 'Sync' version of all the methods
            void BuildPromiseAndSyncMethods(string method)
            {
                var parts = method.Split(':');
                var methodName = parts[0];
                var methodSig = parts[1].Replace("function", "");

                // build the Sync version and turn the original into async
                Engine.Execute(@$"
                    // rename original method to have 'Sync' sufix
                    fs.{methodName}Sync = function{methodSig} {{
                        let ret = fs.___{methodName}{methodSig};
                        __hostExceptionsHandler__.checkAndThrow();
                        return ret;
                    }}
                    fs.___{methodName} = fs.{methodName};

                    // convert the original method to *fake* async
                    fs.{methodName} = function{methodSig.Replace(")", ", callback)")}
                    {{
                        try {{
                            let ret = fs.{methodName}Sync{methodSig};
                            callback(null, ret);
                        }}
                        catch (e) {{
                            callback(e, null);
                        }}
                    }}
                ");

                // build the promise method
                Engine.Execute(@$"
                    fsPromises.{methodName} = function{methodSig}
                    {{
                        return new Promise((resolve, reject) => {{

                            try {{
                                let ret = fs.{methodName}Sync{methodSig};
                                resolve(ret);
                            }}
                            catch (e) {{
                                reject(e);
                            }}
                        }});
                    }}
                ");
            }

            // add promises
            BuildPromiseAndSyncMethods("readFile: (path)");
            BuildPromiseAndSyncMethods("writeFile: (path, data)");
            BuildPromiseAndSyncMethods("appendFile: (path, data)");
            BuildPromiseAndSyncMethods("getType: (path)");
            BuildPromiseAndSyncMethods("rmdir: (path, options)");
            BuildPromiseAndSyncMethods("mkdir: (path)");
            BuildPromiseAndSyncMethods("renameDir: (path, toPath)");
            BuildPromiseAndSyncMethods("renameFile: (path, toPath)");
            BuildPromiseAndSyncMethods("rename: (path, toPath)");
            BuildPromiseAndSyncMethods("unlink: (path)");
            BuildPromiseAndSyncMethods("exists: (path)");
            BuildPromiseAndSyncMethods("readdir: (path)");
            BuildPromiseAndSyncMethods("isdir: (path)");
            BuildPromiseAndSyncMethods("isfile: (path)");
            BuildPromiseAndSyncMethods("stat: (path)");

            // success!
            return true;
        }

        /// <summary>
        /// Normalize and validate a path.
        /// </summary>
        string NormalizePath(string path)
        {
            // convert to full path relative to root
            path = path.Replace('\\', '/');
            var fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(_rootFull, path));

            // if root is locked, make sure didn't attempt to exit it
            if (_cagePath)
            {
                if (!fullPath.StartsWith(_rootFull))
                {
                    throw new ArgumentException($"Path '{path}' is not allowed, because its outside the caged root folder!");
                }
            }

            // return full path
            return fullPath;
        }

        /// <summary>
        /// Once called, the root folder of the filesystem module will be locked and any attempt to access files outside of it will throw exception.
        /// This action is irrevarisble unless you create a new Filesystem module and initialize it again on your v8 engine.
        /// </summary>
        public void LockPathRoot()
        {
            _cagePath = true;
        }

        /// <summary>
        /// Read all text from a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Text read from file.</returns>
        public string ReadAllText(string path)
        {
            try
            {
                path = NormalizePath(path);
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
                return String.Empty;
            }
        }

        /// <summary>
        /// Get all filenames in a path.
        /// </summary>
        /// <param name="path">Folder path.</param>
        /// <returns>List with filenames under path.</returns>
        public string[] GetFiles(string path)
        {
            try
            { 
                path = NormalizePath(path);
                return Directory.GetFileSystemEntries(path).Select(x => System.IO.Path.GetFileName(x)).ToArray();
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
                return new string[] {};
            }
        }

        /// <summary>
        /// Check if a file or folder exists.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>True if exists, false otherwise.</returns>
        public bool Exists(string path)
        {
            try
            { 
                path = NormalizePath(path);
                return File.Exists(path) || Directory.Exists(path);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
                return false;
            }
        }

        /// <summary>
        /// Write all text into a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="data">Data to write.</param>
        public void WriteAllText(string path, string data)
        {
            try
            {
                path = NormalizePath(path);
                File.WriteAllText(path, data);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Append all text into a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="data">Data to write.</param>
        public void AppendAllText(string path, string data)
        {
            try
            { 
                path = NormalizePath(path);
                File.AppendAllText(path, data);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="path">File path to delete.</param>
        public void Delete(string path)
        {
            try
            { 
                path = NormalizePath(path);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                // move dir
                else if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                // not found?
                else
                {
                    throw new FileNotFoundException($"Path '{path}' does not exist!");
                }
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Rename a file or folder.
        /// </summary>
        /// <param name="fromPath">Source path.</param>
        /// <param name="toPath">Dest path.</param>
        public void Move(string fromPath, string toPath)
        {
            try
            { 
                fromPath = NormalizePath(fromPath);
                toPath = NormalizePath(toPath);

                // move file
                if (File.Exists(fromPath))
                {
                    File.Move(fromPath, toPath);
                }
                // move dir
                else if (Directory.Exists(fromPath))
                {
                    Directory.Move(fromPath, toPath);
                }
                // not found?
                else
                {
                    throw new FileNotFoundException($"Path '{fromPath}' does not exist!");
                }
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Get type - either file, dir, or none.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>Type of file / dir at path.</returns>
        public string GetType(string path)
        {
            try
            { 
                path = NormalizePath(path);

                // file
                if (File.Exists(path))
                {
                    return "file";
                }
                // dir
                else if (Directory.Exists(path))
                {
                    return "dir";
                }
                // not found?
                else
                {
                    return "none";
                }
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
                return "none";
            }
        }

        /// <summary>
        /// Rename a file.
        /// </summary>
        /// <param name="fromPath">Source path.</param>
        /// <param name="toPath">Dest path.</param>
        public void MoveFile(string fromPath, string toPath)
        {
            try
            {
                fromPath = NormalizePath(fromPath);
                toPath = NormalizePath(toPath);
                File.Move(fromPath, toPath);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Get file/dir stat.
        /// </summary>
        /// <param name="path">File or directory path.</param>
        /// <returns>File stat object.</returns>
        public FileStat Stat(string path)
        {
            try
            {
                path = NormalizePath(path);
                FileInfo fi = File.Exists(path) ? new FileInfo(path) : null;
                DirectoryInfo di = Directory.Exists(path) ? new DirectoryInfo(path) : null;
                if (fi == null && di == null) { throw new FileNotFoundException($"File '{path}' not found!"); }
                return new FileStat(fi, di);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
                return null;
            }
        }

        /// <summary>
        /// Rename a folder.
        /// </summary>
        /// <param name="fromPath">Source path.</param>
        /// <param name="toPath">Dest path.</param>
        public void MoveDirectory(string fromPath, string toPath)
        {
            try
            { 
                fromPath = NormalizePath(fromPath);
                toPath = NormalizePath(toPath);
                if (File.Exists(fromPath)) { throw new DirectoryNotFoundException($"'{fromPath}' is not a directory!"); }
                Directory.Move(fromPath, toPath);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Create a directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        public void CreateDirectory(string path)
        {
            try
            {
                path = NormalizePath(path);
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }

        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="recursive">If true, will delete folder recursively.</param>
        public void DeleteDirectory(string path, bool recursive)
        {
            try
            {
                path = NormalizePath(path);
                Directory.Delete(path, recursive);
            }
            catch (Exception ex)
            {
                PassExceptionToJs(ex);
            }
        }
    }

    /// <summary>
    /// File stat object.
    /// </summary>
    public class FileStat
    {
        FileInfo _fileInfo;
        DirectoryInfo _dirInfo;
        FileSystemInfo _info;

        /// <summary>
        /// Create the file stat object.
        /// </summary>
        internal FileStat(FileInfo fi, DirectoryInfo di)
        {
            _fileInfo = fi;
            _dirInfo = di;
            _info = fi != null ? fi : di;
        }

        // epoch time
        static DateTime _epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Get last access time in milliseconds since POSIX epoch time.
        /// </summary>
        public long atimeMs => (long)(_info.LastAccessTimeUtc  - _epoch).TotalMilliseconds;

        /// <summary>
        /// Get last modified time in milliseconds since POSIX epoch time.
        /// </summary>
        public long mtimeMs => (long)(_info.LastWriteTimeUtc - _epoch).TotalMilliseconds;

        /// <summary>
        /// Get creation time in milliseconds since POSIX epoch time.
        /// </summary>
        public long birthtimeMs => (long)(_info.CreationTimeUtc - _epoch).TotalMilliseconds;

        /// <summary>
        /// Get last access time in milliseconds since POSIX epoch time.
        /// </summary>
        public DateTime atime => _info.LastAccessTimeUtc;

        /// <summary>
        /// Get last modified time in milliseconds since POSIX epoch time.
        /// </summary>
        public DateTime mtime => _info.LastWriteTimeUtc;

        /// <summary>
        /// Get creation time in milliseconds since POSIX epoch time.
        /// </summary>
        public DateTime birthtime => _info.CreationTimeUtc;

        /// <summary>
        /// Get file extension.
        /// </summary>
        public string extension => _info.Extension;

        /// <summary>
        /// Filename.
        /// </summary>
        public string name => _info.Name;

        /// <summary>
        /// Full name.
        /// </summary>
        public string fullname => _info.FullName;

        /// <summary>
        /// Get if readonly file.
        /// </summary>
        public bool isReadonly() { return _fileInfo != null && _fileInfo.IsReadOnly; }

        /// <summary>
        /// Is it a directory?
        /// </summary>
        public bool isDirectory() { return _dirInfo != null; }

        /// <summary>
        /// Is it a file?
        /// </summary>
        public bool isFile() { return _fileInfo != null; }

        /// <summary>
        /// Get file size (or 0 for folders).
        /// </summary>
        public long size => _fileInfo?.Length ?? 0;

    }
}
