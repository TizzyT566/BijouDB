namespace BijouDB;

public interface IDataType
{
    public void Deserialize(Stream stream);
    public void Serialize(Stream stream);
}