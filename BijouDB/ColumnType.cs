namespace BijouDB;

[Flags]
public enum ColumnType
{
    None = 0,
    Indexed = 1,
    Unique = 3,
}
