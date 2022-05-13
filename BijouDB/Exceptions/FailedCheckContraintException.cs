namespace BijouDB.Exceptions;

public class FailedCheckContraintException : Exception
{
    public FailedCheckContraintException(string message = "The value failed the 'Check' constraint.") : base(message) { }
}