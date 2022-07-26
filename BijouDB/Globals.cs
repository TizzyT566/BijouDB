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

    public static readonly string DefaultPath = Path.GetFullPath("DB");
    public static string DatabasePath { get; set; } = DefaultPath;

    public static bool Logging { get; set; } = false;
    public static int BitMaskSeed { get; set; } = 712247;

    private static int _consoleLock = 0;

    public enum LogLevel
    {
        Error = -1,
        Success = 0,
        Information = 1,
        Warning = 2
    }

    public static Exception Log(this Exception @this, LogLevel level = LogLevel.Error)
    {
        if (Logging)
        {
            SpinWait.SpinUntil(() => Interlocked.Exchange(ref _consoleLock, 1) == 0);
            ConsoleColor prev = Console.ForegroundColor;
            Console.ForegroundColor = level switch
            {
                LogLevel.Success => ConsoleColor.Green,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
            Console.WriteLine(@this.Message);
            Console.ForegroundColor = prev;
            Interlocked.Exchange(ref _consoleLock, 0);
        }
        return @this;
    }
}
