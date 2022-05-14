# BijouDB
A small C# database

Nuget: [BijouDB Package](https://www.nuget.org/packages/BijouDB/)

## Note
Currently this utilizes preview features so to use this you must enable preview features in your project.

```xml
<PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>preview</LangVersion>
    <EnablePreviewFeatures>true</EnablePreviewFeatures>
</PropertyGroup>
```
# Features
## DataTypes => Synonymous Wrapper
```
                              BigInteger => @bint

                                  byte[] => @blob

                                    bool => @bool

                                    byte => @byte

                                    char => @char

                                DateTime => @date

                                 decimal => @decimal

                                   float => @float

                                     int => @int

                                    long => @long

                      R : BijouDB.Record => @record<R>

                                   sbyte => @sbyte

                                   short => @short

                                  string => @string

                                    uint => @uint

                                   ulong => @ulong

                                  ushort => @ushort

                           Tuple<T1, T2> => @tuple<D1, D2>

                       Tuple<T1, T2, T3> => @tuple<D1, D2, D3>

                   Tuple<T1, T2, T3, T4> => @tuple<D1, D2, D3, D4>

               Tuple<T1, T2, T3, T4, T5> => @tuple<D1, D2, D3, D4, D5>

           Tuple<T1, T2, T3, T4, T5, T6> => @tuple<D1, D2, D3, D4, D5, D6>

       Tuple<T1, T2, T3, T4, T5, T6, T7> => @tuple<D1, D2, D3, D4, D5, D6, D7>

Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> => @tuple<D1, D2, D3, D4, D5, D6, D7, DRest>
```

Nullable Types:

```
                              BigInteger? => @bint.nullable

                                  byte[]? => @blob.nullable

                                    bool? => @bool.nullable

                                    byte? => @byte.nullable

                                    char? => @char.nullable

                                DateTime? => @date.nullable

                                 decimal? => @decimal.nullable

                                   float? => @float.nullable

                                     int? => @int.nullable

                                    long? => @long.nullable

                      R? : BijouDB.Record => @record<R>.nullable

                                   sbyte? => @sbyte.nullable

                                   short? => @short.nullable

                                  string? => @string.nullable

                                    uint? => @uint.nullable

                                   ulong? => @ulong.nullable

                                  ushort? => @ushort.nullable

                           Tuple<T1, T2>? => @tuple<D1, D2>.nullable

                       Tuple<T1, T2, T3>? => @tuple<D1, D2, D3>.nullable

                   Tuple<T1, T2, T3, T4>? => @tuple<D1, D2, D3, D4>.nullable

               Tuple<T1, T2, T3, T4, T5>? => @tuple<D1, D2, D3, D4, D5>.nullable

           Tuple<T1, T2, T3, T4, T5, T6>? => @tuple<D1, D2, D3, D4, D5, D6>.nullable

       Tuple<T1, T2, T3, T4, T5, T6, T7>? => @tuple<D1, D2, D3, D4, D5, D6, D7>.nullable

Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>? => @tuple<D1, D2, D3, D4, D5, D6, D7, DRest>.nullable
```

## Constraints
> Unique `Ensures that a column has all unique values`

> Default `Specifies a default for any values that arent set`

> Check `Specifies a condition for a value to be set`

> NotNull is the default, use a `IDataType.nullable` to explicitly use null

> PRIMARY KEY and FOREIGN KEY is replicated loosely with `References`

## Custom DataTypes
You can create your own data types by implementing the interface `BijouDB.IDataType`

```csharp
public interface IDataType
{
    // The size in bytes of the serialized datatype, set to 0 if not a fixed size
    public static abstract long Length { get; }
    // The logic to convert from bytes
    public void Deserialize(Stream stream);
    // The logic to convert to bytes
    public void Serialize(Stream stream);
    // The logic to represent the datatype in a human readable string
    public string ToString();
}
```

# Usage
To get started simply create a new class which inherits from `BijouDB.Record`.

Create a static readonly field to act as a column, specify a BijouDB datatype.

As a convention, the column should end in `Column`.

Create a new property with the respectice type of the column.

The get accessor should call the column you just made's `Get()` method, passing in `this` as an argument.

The set accessor should call the column's `Set()` method, passing in `this` and `value` as arguments.

Create a static contructor, its needed to instantiate the columns.

Use the SchemaBuilder to generate the columns via the `Add()` method.

Do this for each of your columns and finally at the end call the `Build()` method.

## Example
```csharp
using BijouDB;
using BijouDB.DataTypes;

public class MyRecord : Record
{
    public static readonly Column<@int> AgeColumn;
    public int Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    static MyRecord() => SchemaBuilder<MyRecord>
        .Add(out AgeColumn)
        .Build();
}
```

## Specifying Constraints
Use the SchemaBuilder to add constraints to your columns.

Place the constraints on any column by using the `Add()` method.

Provide the label for the constraint `Unique:` or `Default:` or `Check:` followed by the value for the constraint.

The order does't matter and you don't have to provide every constraint.

```csharp
using BijouDB;
using BijouDB.DataTypes;

public class MyRecord : Record
{
    public static readonly Column<@int> AgeColumn;
    public int Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    static MyRecord() => SchemaBuilder<MyRecord>
        // Specify that the column is NOT unique
        // Specify that valid values must be 18 or larger
        // Specify that the default value is 18
        .Add(out AgeColumn, Unique: false, Check: value => value >= 18, Default: () => 18)
        .Build();
}
```

## References
In SQL we have the notion of `PRIMARY KEY` and `FOREIGN KEY` to link relationships between tables.

Here we have the concept of References. Its very similar in concept.

References prevent a Record from being deleted if it has references. 

You can create a `Reference` specifying the `Record` it references and the generic type of that column.

As a convention you should end the field with `References`.

Create a property with the type of the referenced Record. This property should be an array `[]`.

Use the `For()` method passing in `this`.

You then use an overload for the `Add()` method for References. References do NOT have contraints.

Instead you have to specifically point it to the column in the referenced Record.

```csharp
using BijouDB;
using BijouDB.DataTypes;

public class Employee : Record
{
    public static readonly Column<@int> AgeColumn;
    public int Age { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    // A Reference to 'Computer' Record
    public static readonly References<Computer, @record<Employee>> ComputerReferences;
    public Computer[] Computers => ComputerReferences.For(this);

    static Employee() => SchemaBuilder<Employee>
        .Add(out AgeColumn, Unique: false, Check: value => value >= 18, Default: () => 18)
        // Generates the Referennce, and points it to the 'EmployeeColumn' in 'Computer' Record
        .Add(out ComputerReferences, () => Computer.EmployeeColumn)
        .Build();
}

public class Computer : Record
{
    public static readonly Column<@record<Employee>> EmployeeColumn;
    public Employee Employee { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    static Computer() => SchemaBuilder<Computer>
        .Add(out EmployeeColumn)
        .Build();
}
```

To access references of a record, call its respective property.

```cs
Employee employee = new()
{
    Age = 30
};

Computer comp1 = new()
{
    Employee = employee
};

foreach (Computer computer in employee.Computers)
{
    // Manipulate the record here
}
```

## Removing Records
All Records have the following methods to be removed from the database.
```cs
// Removes the Record from the database with no exception handling (you have to do so manually)
public void Remove();
```
```cs
// Tries the remove the Record from the database while suppressing any exceptions.
// true if successfully removed, otherwise false.
public bool TryRemove(); 
```
```cs
// Tries the remove the Record from the database.
// Exposes any Exception that might've thrown.
// true if successfully removed, otherwise false.
public bool TryRemove(out Exception? exception); 
```

## Getting Records
If you know the `Type` and `Id` (represented as a `Guid`) use the `BijouDB.Record.TryGet<R>( ... )` method.

```cs
public static bool TryGet<R>(Guid id, out R? record) { }
public static bool TryGet<R>(string id, out R? record) { }

// Example
// Get a single record via its Id
if (BijouDB.Record.TryGet("0e758669-33ee-847e-d0e8-f5e89cc2b5c2", out Employee? employee))
{
    // Manipulate the record here
}
```

If you know only the `Type` and wish to get all Records of that type you can use `BijouDB.Record.GetAll<R>()` method.

```cs
public static R[] GetAll<R>() { }

// Example
// Gets all Employee records
foreach (Employee employee in BijouDB.Record.GetAll<Employee>())
{
    // Manipulate the record here
}
```

If you know the `Type` of the Record and the value to one of its columns you can call the Column's `RecordsWithValue<R>( .. )` method.

```cs
public R[] RecordsWithValue<R>(D data) { }

// Example
// Gets all Employee records where the age is 19
foreach (Employee employee in Employee.AgeColumn.RecordsWithValue<Employee>(19))
{
    // Manipulate the record here
}
```

## Logging
There was very minimal effor put into logging. It is disabled by default.

To turn on logging, set `BijouDB.Globals.Logging` to `true`.

# Operations Complexity
> Getting a Record via its Id `O(1)`

> Getting all Records of a Type `O(1) - O(n)`

> Getting all Records of a Type by value `O(1) - O(n)`

> Adding a Record `O(1)`

> Removing a Record `O(1) - O(n)`

> Getting References `O(1) - O(n)`