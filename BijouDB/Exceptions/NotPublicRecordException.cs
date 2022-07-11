namespace BijouDB.Exceptions;

public class NotPublicRecordException : Exception
{
    public NotPublicRecordException(Type type) : base($"BijouDB records must be public. '{type}' is not public.") { }
}
