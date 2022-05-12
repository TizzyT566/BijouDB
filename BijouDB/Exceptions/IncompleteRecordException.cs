namespace BijouDB.Exceptions
{
    public class IncompleteRecordException<R> : Exception where R : Record
    {
        public IncompleteRecordException() : base($"Record<{typeof(R).Name}> not yet inserted to store values.") { }
    }
}
