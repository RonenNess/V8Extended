using Microsoft.ClearScript.V8;

// create v8 engine + all modules
V8ScriptEngine _v8 = new();
var _console = new V8Extended.Console();
var _fs = new V8Extended.Filesystem();
var _path = new V8Extended.Path();
var _intervals = new V8Extended.Intervals();

// lock filesystem
_fs.RootFolder = "data";
_fs.LockPathRoot();
if (!Directory.Exists("data")) { Directory.CreateDirectory("data"); }

// add modules to engine
_console.Extend(_v8);
_fs.Extend(_v8);
_path.Extend(_v8);
_intervals.Extend(_v8);

// true if we are currently waiting for a command to finish
bool WaitingForCommand = false;

// queue of commands from C# to js
var commands = new Queue<Command>();
_v8.AddHostObject("getNextCommand", () =>
{
    if (commands.Count == 0) { return null; }
    return commands.Dequeue();
});

// tell C# part we finished with current command
_v8.AddHostObject("finishCommand", () =>
{
    _console.Debug("\nJS side finished handling the command!\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
    Console.ForegroundColor = ConsoleColor.Gray;
    WaitingForCommand = false;
});

// start intervals in current thread
_intervals.StartEventsLoop();

// start the js code (will continue to execute in background thanks to the intervals):
try
{
    _v8.Execute(@"
        // action to perform every 1 ms
        function main() {

            // get next command and skip if don't have anything
            let command = getNextCommand();
            if (!command) { return; }

            // get command
            console.debug('Got command from C#: ' + command.id + '\n');

            // execute commands
            switch (command.id)
            {
                case 'read':
                    readFile(command.args);
                    break;

                case 'write':
                    writeFile(command.args);
                    break;
                
                case 'delete':
                    deleteFile(command.args);
                    break;
                
                case 'dir':
                    dirCommand(command.args);
                    break;

                case 'rmdir':
                    rmDir(command.args);
                    break;

                case 'mkdir':
                    makeDir(command.args);
                    break;

                default:
                    throw new Error('Unknown command!');
            }

            // finish command
            finishCommand();
        }

        // start main loop
        setInterval(main, 1);

        // implement method to read files
        function readFile(args)
        {
            var p = args[0];
            try {
                if (fs.existsSync(p)) {
                    var data = fs.readFileSync(p);
                    console.log('File content:');
                    console.info(data);    
                }
                else {
                    console.error('File not found!');
                }
            }
            catch (e) {
                console.error(e);
            }
        }

        // implement method to delete files
        function deleteFile(args)
        {
            var p = args[0];
            try {
                if (fs.existsSync(p)) {
                    var data = fs.unlinkSync(p);
                    console.log('File deleted.');
                }
                else {
                    console.error('File not found!');
                }
            }
            catch (e) {
                console.error(e);
            }
        }

        // implement method to write files
        function writeFile(args)
        {
            var p = args[0];
            var data = args[1];
            try {
                var data = fs.writeFileSync(p, data);
                console.log('File saved.');
            }
            catch (e) {
                console.error(e);
            }
        }

        // implement method to get files and fodler names
        function dirCommand(args)
        {
            var p = args[0];
            try {
                var files = fs.readdirSync(p);
                for (let file of files) {
                    let prefix = fs.isfileSync(file) ? '[F] ' : '[D] ';
                    console.log(prefix + file);
                }
            }
            catch (e) {
                console.error(e);
            }
        }

        // implement makedir method
        function makeDir(args)
        {
            var p = args[0];
            try {
                fs.mkdirSync(p);
                console.log('Folder created.');
            }
            catch (e) {
                console.error(e);
            }
        }

        // implement remove dir method
        function rmDir(args)
        {
            var p = args[0];
            try {
                if (!fs.existsSync(p)) {
                    console.error('Folder not found!');
                    return;
                }
                if (!fs.isdirSync(p)) {
                    console.error('Not a folder!');
                    return;
                }
                fs.rmdirSync(p);
                console.log('Folder deleted.');
            }
            catch (e) {
                console.error(e);
            }
        }
    ");
}
catch (Exception ex)
{
    Console.WriteLine("Unexpected Error! " + ex);
    Environment.Exit(1);
}

// enqueue command to send to js
void SendCommandToJs(string id, string[] args)
{
    WaitingForCommand = true;
    commands.Enqueue(new Command() { id = id, args = args });
    _console.Debug("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\nEnqueued command to JS: ", id);
    Console.ForegroundColor = ConsoleColor.Gray;
}


// method to print help
void PrintHelp()
{
    Console.WriteLine(@"Possible Commands:
    - 'help': print this message with possible commands.
    - 'read': read a file.
    - 'write': write a file.
    - 'delete': delete a file.
    - 'dir': show files and folder names.
    - 'mkdir': create a folder.
    - 'rmdir' remove a folder.
    - 'exit': exit application.
");
}

// exit application
void ExitApp()
{
    Console.WriteLine("Goodbye!");
    Environment.Exit(0);
}

// read file command
void ReadFileCommand()
{
    Console.Write("File path: ");
    var path = Console.ReadLine();
    SendCommandToJs("read", new string[] { path });
}

// write file command
void WriteFileCommand()
{
    Console.Write("File path: ");
    var path = Console.ReadLine();
    Console.Write("Data to write: ");
    var data = Console.ReadLine();
    SendCommandToJs("write", new string[] { path, data });
}

// dir command
void DirCommand()
{
    Console.Write("Dir path (empty for cwd): ");
    var path = Console.ReadLine();
    SendCommandToJs("dir", new string[] { path });
}

// make dir command
void MakeDirCommand()
{
    Console.Write("Dir path: ");
    var path = Console.ReadLine();
    SendCommandToJs("mkdir", new string[] { path });
}

// remove dir command
void RemoveDirCommand()
{
    Console.Write("Dir path: ");
    var path = Console.ReadLine();
    SendCommandToJs("rmdir", new string[] { path });
}

// delete file command
void DeleteFileCommand()
{
    Console.Write("File path: ");
    var path = Console.ReadLine();
    SendCommandToJs("delete", new string[] { path });
}

// print welcome message
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine(@"Welcome to V8.Extended demo!
This demo is an interactive shell that can perform some IO actions from within the V8 engine.
The C# part is only responsible for user input and output, the JS script implements everything.

Note: for files related operations you are not allowed to exit the working folder. Attempts to do so will result in an exception.
");
PrintHelp();

Console.ForegroundColor= ConsoleColor.Gray;
while (true)
{
    // wait for commands
    if (WaitingForCommand)
    {
        Thread.Sleep(10);
        continue;
    }

    // get command
    Console.WriteLine("\nEnter command to execute:");
    Console.Write(">> ");
    var command = Console.ReadLine();

    // parse command
    switch (command)
    {
        case "exit":
            ExitApp();
            break;

        case "help":
            PrintHelp();
            break;

        case "read":
            ReadFileCommand();
            break;

        case "write":
            WriteFileCommand();
            break;

        case "dir":
            DirCommand();
            break;

        case "mkdir":
            MakeDirCommand();
            break;

        case "rmdir":
            RemoveDirCommand();
            break;

        case "delete":
            DeleteFileCommand();
            break;

        default:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Unknown command! Type 'help' for possible commands.");
            Console.ForegroundColor = ConsoleColor.Gray;
            break;
    }
}

// to transfer commands from C# to js
public class Command
{
    public string id;
    public string[] args;
};