namespace BijouDB.Exceptions
{
    internal class UniquenessConstraintException<D> : Exception where D : IDataType
    {
        public UniquenessConstraintException() : base($"Value already exists in a {typeof(D).FullName} column with the 'Unique' constraint.") { }
    }
}
