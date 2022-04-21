namespace BijouDB.Exceptions
{
    public class InvalidProtectedDataTypeException : Exception
    {
        public InvalidProtectedDataTypeException() : base("Protected columns must be nullable.") { }
    }
}
