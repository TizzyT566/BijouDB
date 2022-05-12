namespace BijouDB.Exceptions
{
    public class IncompleteRecordException<T> : Exception where T : Record
    {
        public IncompleteRecordException() : base($"Record<{typeof(T).Name}> not yet inserted to store values.") { }
    }
}
