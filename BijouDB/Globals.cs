namespace BijouDB;

public static class Globals
{
    public static readonly string DB_Path = Path.GetFullPath("DB");
    public static readonly string ColName = "col";
    public static readonly string Rec = "rcrd";
    public static readonly string Index = "indx";
    public static readonly string Ref = "ref";
    public static readonly string RefPattern = $"*.{Ref}";
    public static readonly string RecPattern = $"*.{Rec}";
    public static readonly string BinFile = "value.bin";

    public static bool Logging { get; set; } = false;
    public static int SeedMask { get; set; } = 712247;
}
