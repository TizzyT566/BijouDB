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
}
