namespace BijouDB.Exceptions
{
    public class IncompleteRecordException<T> : Exception where T : Table
    {
        public IncompleteRecordException() : base($"Record<{typeof(T).Name}> is missng values before it can be stored.") { }
    }
}
