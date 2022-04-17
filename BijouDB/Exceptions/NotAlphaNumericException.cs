namespace BijouDB.Exceptions;

public class NotAlphaNumericException : Exception
{
    public NotAlphaNumericException(string offendingString) : base($"{offendingString} is not alphanumeric.") { }
}
