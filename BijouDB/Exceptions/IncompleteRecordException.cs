namespace BijouDB.Exceptions
{
    public class IncompleteRecordException<T> : Exception where T : Schema
    {
        public IncompleteRecordException() : base($"Record<{typeof(T).Name}> not yet inserted to store values.") { }
    }
}
