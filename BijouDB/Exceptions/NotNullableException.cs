namespace BijouDB.Exceptions;

public class NotNullableException: Exception
{
    public NotNullableException(string type) : base($"Null not allowed for ( @{type} ), try ( @{type}.nullable ).") { }
}
