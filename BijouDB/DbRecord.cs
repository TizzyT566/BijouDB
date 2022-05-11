using System.Reflection;

namespace BijouDB
{
    public class DbTable
    {

    }

    public abstract record DbRecord
    {
        private readonly static Dictionary<Type, DbTable> _dbTableColumns;

        public readonly Guid Id;

        public DbRecord() => Id = IncrementalGuid.NextGuid();

        private DbRecord(Guid id) => Id = id;

        // Static contructor to setup all tables and schemas
        static DbRecord()
        {
            _dbTableColumns = new();

            Type recordBaseType = typeof(DbRecord);
            Assembly? assembly = Assembly.GetEntryAssembly();
            if (assembly is not null)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsAbstract && type.IsSubclassOf(recordBaseType) && type.FullName != null)
                    {
                        //Console.WriteLine(type.FullName);

                        // Get all properties
                        PropertyInfo[] properties = type.GetProperties();

                        // check if properties are all IDataTypes
                        foreach(PropertyInfo property in properties)
                        {
                            Console.WriteLine(property.Name);
                        }

                        // check if properties have attributes

                        // construct columns based on attributes
                    }
                }
            }
        }

        public void Meh()
        {
            Console.WriteLine(GetType().FullName);
        }
    }
}
