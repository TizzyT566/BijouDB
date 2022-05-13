namespace BijouDB.Exceptions;

internal class UniqueConstraintException<D> : Exception where D : IDataType
{
    public UniqueConstraintException() : base($"Value already exists in a {typeof(D).FullName} column with the 'Unique' constraint.") { }
}
