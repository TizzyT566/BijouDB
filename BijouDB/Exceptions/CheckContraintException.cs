namespace BijouDB.Exceptions;

public class CheckContraintException : Exception
{
    public CheckContraintException(string message = "The value failed the 'Check' constraint.") : base(message) { }
}