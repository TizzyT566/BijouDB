namespace BijouDB.Exceptions;

public class IncompleteSchemaException : Exception
{
    public IncompleteSchemaException(Type type) : base($"The schema {type.FullName} was left incomplete. Call the 'Build()' method at the end to complete the schema.") { }
}
