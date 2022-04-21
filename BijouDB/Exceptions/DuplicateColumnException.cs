namespace BijouDB.Exceptions
{
    public class DuplicateColumnException : Exception
    {
        public DuplicateColumnException(string columnName) : base($"Column {columnName} already exists.") { }
    }
}
