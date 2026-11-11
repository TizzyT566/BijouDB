using System.Reflection;

namespace BijouDB;

public static class Globals
{
    internal static readonly string ColName = "col";
    internal static readonly string Rec = "rcrd";
    internal static readonly string Index = "indx";
    internal static readonly string Ref = "ref";
    internal static readonly string RefPattern = $"*.{Ref}";
    internal static readonly string RecPattern = $"*.{Rec}";
    internal static readonly string BinFile = "value.bin";

    public static readonly string DefaultPath = Path.Combine(
        string.IsNullOrEmpty(typeof(Globals).Assembly.Location) ? AppContext.BaseDirectory : Path.GetDirectoryName(typeof(Globals).Assembly.Location)!, "DB");

    private static string _databasePath = DefaultPath;
    private static int _bitMaskSeed = 712247;

    /// <summary>
    /// Configure before first database access. Settings freeze after initialization.
    /// </summary>
    public static string DatabasePath
    {
        get => _databasePath;
        set
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A database path is required.", nameof(value));

            lock (Storage.SyncRoot)
            {
                string path = Path.GetFullPath(value);
                if (Storage.IsInitialized && !string.Equals(path, _databasePath, Storage.PathComparison))
                    throw new InvalidOperationException("DatabasePath cannot change after database initialization.");
                _databasePath = path;
            }
        }
    }

    public static int BitMaskSeed
    {
        get => _bitMaskSeed;
        set
        {
            lock (Storage.SyncRoot)
            {
                if (Storage.IsInitialized && value != _bitMaskSeed) throw new InvalidOperationException("BitMaskSeed cannot change after database initialization.");
                _bitMaskSeed = value;
            }
        }
    }

    public static bool Logging { get; set; }

    /// <summary>
    /// Optional callback; logging failures never replace the original database error.
    /// </summary>
    public static Action<Exception, LogLevel>? Logger { get; set; }

    private static readonly object _consoleLock = new();

    public enum LogLevel
    {
        Error = -1,
        Success = 0,
        Information = 1,
        Warning = 2
    }

    public static Exception Log(this Exception exception, LogLevel level = LogLevel.Error)
    {
        try
        {
            if (Logger is not null)
                Logger(exception, level);
            else if (Logging)
                lock (_consoleLock)
                    Console.Error.WriteLine(exception.Message);
        }
        catch
        { /* Diagnostics must not replace the error being reported. */
        }

        return exception;
    }
}
