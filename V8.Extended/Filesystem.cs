
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
    // read all data as text, sync
    readFileSync: function(path)
    {
        let ret = __Filesystem__.ReadAllText(path);
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // read all data as text
    readFile: function(path, cb)
    {
        __Filesystem__.ReadAllTextCb(path, cb);
    },

    // write all data as text, sync
    writeFileSync: function(path, data)
    {
        __Filesystem__.WriteAllText(path, data);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // write all data as text
    writeFile: function(path, data, cb)
    {
        __Filesystem__.WriteAllTextCb(path, data, cb);
    },

    // append all data as text, sync
    appendFileSync: function(path, data)
    {
        __Filesystem__.AppendAllText(path, data);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // append all data as text
    appendFile: function(path, data, cb)
    {
        __Filesystem__.AppendAllTextCb(path, data, cb);
    },

    // get file names in dir, sync
    readdirSync: function(path)
    {
        let ret = __Filesystem__.GetFiles(path);
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // get file names in dir
    readdir: function(path, cb)
    {
        __Filesystem__.GetFilesCb(path, cb);
    },

    // check if a file exists, sync
    existsSync: function(path)
    {
        let ret = __Filesystem__.Exists(path);
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // check if a file exists
    exists: function(path, cb)
    {
        __Filesystem__.ExistsCb(path, cb);
    },

    // delete a file, sync
    unlinkSync: function(path)
    {
        __Filesystem__.Delete(path);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // delete a file
    unlink: function(path, cb)
    {
        __Filesystem__.DeleteCb(path, cb);
    },

    // rename a file or folder, sync
    renameSync: function(path, toPath)
    {
        __Filesystem__.Move(path, toPath);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // rename a file or folder
    rename: function(path, toPath, cb)
    {
        __Filesystem__.Move(path, toPath, cb);
    },

    // rename a file, sync
    renameFileSync: function(path, toPath)
    {
        __Filesystem__.MoveFile(path, toPath);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // rename a file
    renameFile: function(path, toPath, cb)
    {
        __Filesystem__.MoveFileCb(path, toPath, cb);
    },

    // rename a directory, sync
    renameDirSync: function(path, toPath)
    {
        __Filesystem__.MoveDirectory(path, toPath);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // rename a directory
    renameDir: function(path, toPath, cb)
    {
        __Filesystem__.MoveDirectoryCb(path, toPath, cb);
    },

    // create a folder, sync
    mkdirSync: function(path)
    {
        __Filesystem__.CreateDirectory(path);
        __hostExceptionsHandler__.checkAndThrow();
    },

    // create a folder
    mkdir: function(path, cb)
    {
        __Filesystem__.CreateDirectoryCb(path, cb);
    },

    // delete a folder, sync
    rmdirSync: function(path, options) 
    {
        __Filesystem__.DeleteDirectory(path, Boolean(options && options.recursive));
        __hostExceptionsHandler__.checkAndThrow();
    },

    // delete a folder
    rmdir: function(path, options, cb) 
    {
        if (options) {
            __Filesystem__.DeleteDirectoryCb(path, Boolean(options.recursive), cb);
        }
        else {
            __Filesystem__.DeleteDirectoryCb(path, false, options);
        }
    },

    // get file or folder type, returning one of: 'file', 'dir', 'none', sync.
    getTypeSync: function(path)
    {
        let ret = __Filesystem__.GetType(path);
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // get file or folder type, returning one of: 'file', 'dir', 'none'.
    getType: function(path, cb)
    {
        __Filesystem__.GetTypeCb(path, cb);
    },

    // check if path is a folder, sync
    isdirSync: function(path)
    {
        let ret = this.getTypeSync(path) === 'dir';
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // check if path is a folder
    isdir: function(path, cb)
    {
        this.getType(path, (err, res) => {
            cb(err, res === 'dir');
        });
    },

    // check if path is a file, sync
    isfileSync: function(path)
    {
        let ret = this.getTypeSync(path) === 'file';
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // check if path is a file
    isfile: function(path)
    {
        this.getType(path, (err, res) => {
            cb(err, res === 'file');
        });
    },

    // get file stat, sync
    statSync: function(path)
    {
        let ret = __Filesystem__.Stat(path);
        __hostExceptionsHandler__.checkAndThrow();
        return ret;
    },

    // get file stat
    stat: function(path, cb)
    {
        __Filesystem__.StatCb(path, cb);
    },
}

// will be later filled with the 'promises' versions of the api
var fsPromises = {};
");

            // build the 'promises' version of a method
            void BuildPromiseMethod(string method)
            {
                // get method name and signature
                var parts = method.Split(':');
                var methodName = parts[0];
                var methodSig = parts[1].Replace("function", "");

                // build the promise method on top of the async version of the methods
                Engine.Execute(@$"
                    fsPromises.{methodName} = function{methodSig}
                    {{
                        return new Promise((resolve, reject) => {{

                            try {{
                                fs.{methodName}{methodSig.TrimEnd(')')}, (err, ret) => {{
                                    if (err) {{ reject(err); }}
                                    else {{ resolve(ret); }}
                                }});
                            }}
                            catch (e) {{
                                reject(e);
                            }}
                        }});
                    }}
                ");
            }

            // add promises
            BuildPromiseMethod("readFile: (path)");
            BuildPromiseMethod("writeFile: (path, data)");
            BuildPromiseMethod("appendFile: (path, data)");
            BuildPromiseMethod("getType: (path)");
            BuildPromiseMethod("rmdir: (path, options)");
            BuildPromiseMethod("mkdir: (path)");
            BuildPromiseMethod("renameDir: (path, toPath)");
            BuildPromiseMethod("renameFile: (path, toPath)");
            BuildPromiseMethod("rename: (path, toPath)");
            BuildPromiseMethod("unlink: (path)");
            BuildPromiseMethod("exists: (path)");
            BuildPromiseMethod("readdir: (path)");
            BuildPromiseMethod("isdir: (path)");
            BuildPromiseMethod("isfile: (path)");
            BuildPromiseMethod("stat: (path)");

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

        /// <summary>
        /// Read all text from a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <returns>Text read from file.</returns>
        public async Task<string> ReadAllTextCb(string path, dynamic cb)
        {
            try
            {
                path = NormalizePath(path);
                var ret = await File.ReadAllTextAsync(path);
                cb(null, ret);
                return ret;
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
                return String.Empty;
            }
        }

        /// <summary>
        /// Get all filenames in a path.
        /// </summary>
        /// <param name="path">Folder path.</param>
        /// <returns>List with filenames under path.</returns>
        public async Task<string[]> GetFilesCb(string path, dynamic cb)
        {
            try
            {
                path = NormalizePath(path);
                var ret = await Task.Run(() => Directory.GetFileSystemEntries(path).Select(x => System.IO.Path.GetFileName(x)).ToArray());
                cb(null, ret);
                return ret;
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
                return new string[] { };
            }
        }

        /// <summary>
        /// Check if a file or folder exists.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>True if exists, false otherwise.</returns>
        public async Task<bool> ExistsCb(string path, dynamic cb)
        {
            try
            {
                path = NormalizePath(path);
                var ret = await Task.Run(() => File.Exists(path) || Directory.Exists(path));
                cb(null, ret);
                return ret;
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
                return false;
            }
        }

        /// <summary>
        /// Write all text into a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="data">Data to write.</param>
        public async Task WriteAllTextCb(string path, string data, dynamic cb)
        {
            try
            {
                path = NormalizePath(path);
                await File.WriteAllTextAsync(path, data);
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Append all text into a file.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="data">Data to write.</param>
        public async Task AppendAllTextCb(string path, string data, dynamic cb)
        {
            try
            {
                path = NormalizePath(path);
                await File.AppendAllTextAsync(path, data);
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="path">File path to delete.</param>
        public async Task DeleteCb(string path, dynamic cb)
        {
            try
            {
                await Task.Run(() => this.Delete(path));
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Rename a file or folder.
        /// </summary>
        /// <param name="fromPath">Source path.</param>
        /// <param name="toPath">Dest path.</param>
        public async Task MoveCb(string fromPath, string toPath, dynamic cb)
        {
            try
            {
                await Task.Run(() => this.Move(fromPath, toPath));
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Get type - either file, dir, or none.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>Type of file / dir at path.</returns>
        public async Task<string> GetTypeCb(string path, dynamic cb)
        {
            try
            {
                var ret = await Task.Run(() => this.GetType(path));
                cb(null, ret);
                return ret;
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
                return "none";
            }
        }

        /// <summary>
        /// Rename a file.
        /// </summary>
        /// <param name="fromPath">Source path.</param>
        /// <param name="toPath">Dest path.</param>
        public async Task MoveFileCb(string fromPath, string toPath, dynamic cb)
        {
            try
            {
                await Task.Run(() => this.MoveFile(fromPath, toPath));
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Get file/dir stat.
        /// </summary>
        /// <param name="path">File or directory path.</param>
        /// <returns>File stat object.</returns>
        public async Task<FileStat> StatCb(string path, dynamic cb)
        {
            try
            {
                var ret = await Task.Run(() => this.Stat(path));
                cb(null, ret);
                return ret;
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
                return null;
            }
        }

        /// <summary>
        /// Rename a folder.
        /// </summary>
        /// <param name="fromPath">Source path.</param>
        /// <param name="toPath">Dest path.</param>
        public async Task MoveDirectoryCb(string fromPath, string toPath, dynamic cb)
        {
            try
            {
                await Task.Run(() => this.MoveDirectory(fromPath, toPath));
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Create a directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        public async Task CreateDirectoryCb(string path, dynamic cb)
        {
            try
            {
                await Task.Run(() => this.CreateDirectory(path));
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
            }
        }

        /// <summary>
        /// Delete a directory.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="recursive">If true, will delete folder recursively.</param>
        public async Task DeleteDirectoryCb(string path, bool recursive, dynamic cb)
        {
            try
            {
                await Task.Run(() => this.DeleteDirectory(path, recursive));
                cb(null, null);
            }
            catch (Exception ex)
            {
                cb(ex.ToString(), null);
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
