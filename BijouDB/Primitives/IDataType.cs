namespace BijouDB.Primitives;

public interface IDataType
{
    public static abstract long Length { get; }
    public void Deserialize(Stream stream);
    public void Serialize(Stream stream);
}
