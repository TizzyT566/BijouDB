> :bangbang: **BijouDB 4.0.0.0 is NOT backwards compatible** :bangbang:

![alt text](https://raw.githubusercontent.com/TizzyT566/BijouDB/master/BijouDB/Icon.png "BijouDB")

# BijouDB
A small C# database

Nuget: [BijouDB Package](https://www.nuget.org/packages/BijouDB/)

# Features
## DataTypes => Synonymous Wrapper
```
                                 BigInteger => @bint

                                     byte[] => @blob

                                       bool => @bool

                                       byte => @byte

                                       char => @char

                                   DateTime => @time

                                    decimal => @decimal

                                     double => @double

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

                                   DateTime? => @time.nullable

                                    decimal? => @decimal.nullable

                                     double? => @double.nullable

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

> Default `Specifies a default for unset values`

> Check `Specifies a condition for a value to be set`

> NotNull is the default, use a `IDataType.nullable` to explicitly use null

> PRIMARY KEY and FOREIGN KEY is replicated loosely with `Reference`

## Custom DataTypes
You can create your own data types by implementing the interface `BijouDB.IDataType`

```csharp
public interface IDataType
{
    // The logic to convert from bytes
    public void Deserialize(Stream stream);
    // The logic to convert to bytes
    public void Serialize(Stream stream);
}
```

# Usage
To get started simply create a new class which inherits from `BijouDB.Record`.

Create a static readonly field to act as a column, specify a BijouDB datatype.

As a convention, the column should end in `Column`.

Create a new property with the respective type of the column (check the datatypes chart above).

The get accessor should call the column you just made's `Get()` method, passing in `this` as an argument.

The set accessor should call the column's `Set()` method, passing in `this` and `value` as arguments.

Create a static contructor, its needed to instantiate the columns.

Use the SchemaBuilder to generate the columns via the `Add()` method.

Do this for each of your columns and finally preceed the SchemaBuilder with `_ = ~`.

This pattern automatically disposes the SchemaBuilder at the end and ensures proper init and disposal.

## Example
```csharp
using BijouDB;
using BijouDB.DataTypes;

public class MyRecord : Record
{
    public static readonly Column<@int> AgeColumn;

    [Json] public int Age
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    static MyRecord() => _ = ~SchemaBuilder<MyRecord>
        .Add(out AgeColumn);
}
```

## Specifying Constraints
Use the SchemaBuilder to add constraints to your columns.

Place the constraints on columns by passing arguments to the `Add()` method.

Provide the label of the constraint `Unique:` or `Default:` or `Check:` followed by the value for the constraint.

The order does't matter and you don't have to provide every constraint.

```csharp
using BijouDB;
using BijouDB.DataTypes;

public class MyRecord : Record
{
    public static readonly Column<@int> AgeColumn;

    [Json] public int Age
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    static MyRecord() => _ = ~SchemaBuilder<MyRecord>
        // Specify that the column is NOT unique
        // Specify that valid values must be 18 or larger
        // Specify that the default value is 18
        .Add(out AgeColumn, Unique: false, Check: (record, value) => value >= 18, Default: () => 18);
}
```

## References
In SQL we have `PRIMARY KEY` and `FOREIGN KEY` to link relationships between tables.

Here we have the concept of  references. Its very similar in concept but is a one-many relationship.

References prevent a Record from being deleted if it has child references.

You can keep the relationship but allow deleting even if references exist by setting the `restricted`

parameter to false in the `Add( ... , bool restricted)` method for references, default is true.

You can create a `Reference` specifying the Column type you want to reference and the `Record`

that column exists in.

As a convention you should end the field with `References`.

Create a property with the type of the referenced Record. This property should be an array `[]`.

Use the `To()` method passing in `this`.

You then use an overload for the `Add()` method for references. References do NOT have contraints.

Instead you have to specifically point it to the column in the referenced Record.

```csharp
using BijouDB;
using BijouDB.DataTypes;

public class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Column<@int> AgeColumn;
    // A Reference to 'Computer' Record
    public static readonly Reference<@record<Employee>, Computer> ComputerReferences;

    [Json] public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    [Json] public int Age
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }
    
    [Json] public IEnumerable<Computer> Computers => ComputerReferences.To(this);

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn)
        .Add(out AgeColumn, Unique: false, Check: value => value >= 18, Default: () => 18)
        // Generates the Referennce, and points it to the 'EmployeeColumn' in 'Computer' Record
        .Add(out ComputerReferences, () => Computer.EmployeeColumn);
}

public class Computer : Record
{
    public static readonly Column<@record<Employee>> EmployeeColumn;

    [Json] public Employee Employee 
    { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeColumn);
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

## Relational

If you want many-to-many relationships than the `Relational` column might be what you want.

It doesn't store values like `Column` nor is it based on values in another table like `Reference`.

It simply creates links between records.

To create a 'Relational' you define a Relational of the current record type and the related record type.

You then add a property of type `Junc` which is nested inside of the Relational column you just made.

The getter should call the relational column you just made's `To()` method passing in `this`.

The setter should be left empty.

Like the other columns you use the SchemaBuilder to instantiate a relational column as well.

Its similar to reference columns but intead of pointing to a `Column<>` you will be pointing

to another `Relational<,>`.

In the other record you will be doing the same thing, so in the end you will have the two

relational columns point to each other.

```cs
// Example

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Column<@int> NumberColumn;
    public static readonly Column<@long> AgeColumn;
    public static readonly Column<@bint> PointsColumn;
    public static readonly Column<@record<Employee>> ManagerColumn;
    public static readonly Column<tuple<@int, @int, @int>> PhoneNumberColumn;
    // Relational Many-To-Many
    public static readonly Relational<Employee, Computer> ComputerRelational;

    [Json] public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    [Json] public int Number 
    { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }

    [Json] public long Age
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }
        
    [Json] public BigInteger Points
    { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value); }
        
    [Json] public Employee Manager
    { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }

    [Json, TupleObject("Area", "Exchange", "Subscriber")]
    public (int, int, int) PhoneNumber
    { get => PhoneNumberColumn.Get(this); set => PhoneNumberColumn.Set(this, value); }
        
    // The junction between this record and 'Computer' records
    [Json] public Relational<Employee, Computer>.Junc Computers
    { get => ComputerRelational.To(this); set { } }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn, Unique: false)
        .Add(out NumberColumn, Default: () => 5555555)
        .Add(out AgeColumn, Check: value => value >= 18)
        .Add(out PointsColumn)
        .Add(out ManagerColumn)
        .Add(out PhoneNumberColumn)
        .Add(out ComputerRelational, Computer.EmployeeRelational);
}

public sealed class Computer : Record
{
    // Relational Many-To-Many
    public static readonly Relational<Computer, Employee> EmployeeRelational;
    public static readonly Column<@string> TypeColumn;

    [Json] public string Type
    { get => TypeColumn.Get(this); set => TypeColumn.Set(this, value); }
        
    // The junction between this record and employee records
    [Json] public Relational<Computer, Employee>.Junc Employees
    { get => EmployeeRelational.To(this); set { } }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeRelational, Employee.ComputerRelational)
        .Add(out TypeColumn, Check: value => value != "" && value != "Dell");
}

// Usage

Employee employee = new();
employee.Computers += new Computer() { Type = "Alienware" };

Computer computer = new() { Type = "Origin" };
computer.Employees += employee;

foreach (Computer comp in employee.Computers.All)
    Console.WriteLine(comp.Type);
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
public static IEnumerable<R> GetAll<R>() { }

// Example
// Gets all Employee records
foreach (Employee employee in BijouDB.Record.GetAll<Employee>())
{
    // Manipulate the record here
}
```

If you know the `Type` of the Record and the value to one of its columns you can call the Column's

`WithValue<R>( ... )` method.

```cs
public IEnumerable<R> WithValue<R>(D data) { }

// Example
// Gets all Employee records where the age is 19
foreach (Employee employee in Employee.AgeColumn.WithValue<Employee>(19))
{
    // Manipulate the record here
}
```

If you know the Type and value of multiple columns then you use the previous method along side `BijouDB.Record.WithValues()` to get all records matching the values you know.

```cs
public static IEnumerable<R> WithValues<R>(params IEnumerabl<R>[] columnMatches) { }

// Example
// Find Employees with the name 'TizzyT' and that are 30 years old
foreach(Employee employee in Record.WithValues<Employee>(
    Employee.NameColumn.WithValue<Employee>("TizzyT"), // Get all records with Name 'TizzyT'
    Employee.AgeColumn.WithValue<Employee>(30) // Get all records with age 30
))
{
    // Manipulate record here
}
```

If you want to know what unique values a column has you can call the respective column's `UniqueValues()` method.

This will give you an array of all unique values found in that column.

```cs
public IEnumerable<D> UniqueValues() { }

// Example
// Get a list of all unique ages in Employee.AgeColumn
foreach (int age in Employee.AgeColumn.UniqueValues())
{
    // Manipulate the age here
}
```

## Json

Records can be presented in Json format by calling the `Json` property.

Columns must be marked with the `JsonAttribute` to be included in the resulting json.

For records with tuple properties you can assign labels by marking the property with the 

`TupleObjectAttribute` and passing in the label names as arguments.

The property will be presented as an object instead of the default for tuples which is an array.

For user defined types you can add formatters via `Json.TryAddFormatter( ... )`.

```cs
public static bool TryAddFormatter(Type type, Func<object, string> formater) { }

// Example

if (Json.TryAddFormatter(typeof(TimeSpan), obj => $"{{TimeSpan:\"{obj}\"}}"))
    Console.WriteLine("Successfully added formatter.");
else
    Console.WriteLine("Failed to add formatter.");
```

```cs
// Example

public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Column<@int> NumberColumn;
    public static readonly Column<@long> AgeColumn;
    public static readonly Column<@bint> PointsColumn;
    public static readonly Column<@record<Employee>> ManagerColumn;
    public static readonly Column<tuple<@int, @int, @int>> PhoneNumberColumn;
    public static readonly Reference<@record<Employee>.nullable, Computer> ComputerReferences;

    // Marked with 'JsonAttribute'
    [Json] public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    // Marked with 'JsonAttribute'
    [Json] public int Number
    { get => NumberColumn.Get(this); set => NumberColumn.Set(this, value); }

    // Marked with 'JsonAttribute'
    [Json] public long Age
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }
        
    // Marked with 'JsonAttribute'
    [Json] public BigInteger Points
    { get => PointsColumn.Get(this); set => PointsColumn.Set(this, value); }
        
    // Marked with 'JsonAttribute'
    [Json] public Employee Manager
    { get => ManagerColumn.Get(this); set => ManagerColumn.Set(this, value!); }

    // Marked with 'JsonAttribute' and 'TupleObjectAttribute'    
    [TupleObject("Area", "Exchange", "Subscriber")]
    [Json] public (int, int, int) PhoneNumber
    { get => PhoneNumberColumn.Get(this); set => PhoneNumberColumn.Set(this, value); }

    // Marked with 'JsonAttribute'
    [Json] public IEnumerable<Computer?> Computers => ComputerReferences.For(this);

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn, Unique: false)
        .Add(out NumberColumn, Default: () => 5555555)
        .Add(out AgeColumn, Check: value => value >= 18)
        .Add(out PointsColumn)
        .Add(out ManagerColumn)
        .Add(out PhoneNumberColumn)
        .Add(out ComputerReferences, () => Computer.EmployeeColumn);
}

public sealed class Computer : Record
{
    public static readonly Column<@record<Employee>.nullable> EmployeeColumn;
    public static readonly Column<@string> TypeColumn;

    // Marked with 'JsonAttribute'
    [Json] public Employee? Employee
    { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    // Marked with 'JsonAttribute'
    [Json] public string Type
    { get => TypeColumn.Get(this); set => TypeColumn.Set(this, value); }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeColumn)
        .Add(out TypeColumn, Check: value => value != "" && value != "Dell");
}

// Usage

Employee test = new()
{
    Name = "TizzyT",
    Points = BigInteger.Parse("672387164454987434695431591434216"),
    PhoneNumber = (555, 856, 8153)
};

Computer com1 = new()
{
    Employee = test,
    Type = "Alienware"
};

Computer com2 = new()
{
    Employee = test,
    Type = "HP"
};

Computer com3 = new()
{
    Employee = test,
    Type = "Origin"
};

Console.WriteLine(test.Json);
```

Output (formatted, actual output is minimal):

```json
{
	"Id": "00004844-0000-0000-0000-000000000000",
	"BijouDB_Test.Tables.Employee": {
		"Name": "TizzyT",
		"Number": 5555555,
		"Age": 0,
		"Points": 672387164454987434695431591434216,
		"Manager": null,
		"PhoneNumber": {
			"Area": 555,
			"Exchange": 856,
			"Subscriber": 8153
		},
		"Computers": [{
			"Id": "00004846-0000-0000-0000-000000000000",
			"BijouDB_Test.Tables.Computer": {
				"Employee": {
					"Id": "00004844-0000-0000-0000-000000000000",
					"BijouDB_Test.Tables.Employee": {}
				},
				"Type": "Alienware"
			}
		}, {
			"Id": "00004848-0000-0000-0000-000000000000",
			"BijouDB_Test.Tables.Computer": {
				"Employee": {
					"Id": "00004844-0000-0000-0000-000000000000",
					"BijouDB_Test.Tables.Employee": {}
				},
				"Type": "HP"
			}
		}, {
			"Id": "00004849-0000-0000-0000-000000000000",
			"BijouDB_Test.Tables.Computer": {
				"Employee": {
					"Id": "00004844-0000-0000-0000-000000000000",
					"BijouDB_Test.Tables.Employee": {}
				},
				"Type": "Origin"
			}
		}]
	}
}
```

If you require more control over how much and what data to show then you can use the 'ToJson()' method.

It has two parameters.

1) Depth : How deep should the json serialization go when returning data.

2) Level : What is the maximum level for properties to be included in the serialization.

## Storing Data

The database can be made to store arbitrary data.

To mitigate a vulnerability due to malicious data being stored, data is stored with a random bitmask.

The mask is generated using a seed. The seed can be changed at `BijouDB.Globals.SeedMask`, default is 712247. 

## Logging
There was very minimal effor put into logging. It is disabled by default.

To turn on logging, set `BijouDB.Globals.Logging` to `true`.

# Operations Complexity
> Getting a Record via its Id `O(1)`

> Getting all Records of a Type `O(1) - O(n)`

> Getting all Records of a Type by value `O(1) - O(n)`

> Adding a Record `O(1)`

> Removing a Record `O(1)`

> Getting References `O(1) - O(n)`

> Getting Relationals `O(log n) - O(n)`

# Gotchas
### New Records are only stored once accessed (either by reading a property, or setting a property)
```
This prevents users from creating empty records and/or creating records which aren't used which
will clutter up the database. This behavior though maybe undesireable in certain situations
and in which case simply accessing the records Id property will store the record in the database.
```
### Only explicitly set values are indexed, This is a performance related design decision
```
This means that during lookups for default values like null, 0, "", '\0', etc will NOT contain any
records which do not have the respective property set. This behavior will be remedied if/when
'Required Properties' feature is out but until then you can simply create a parameterless public
constructor setting any/all the properties you want.
```
```cs
// Example
public sealed class Computer : Record
{
    public static readonly Column<@record<Employee>.nullable> EmployeeColumn;

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeColumn);

    [Json]
    public Employee? Employee
    { get => EmployeeColumn.Get(this); set => EmployeeColumn.Set(this, value); }

    // Contructor which explicitly sets properties upon construction
    public Computer()
    {
        Employee = default;
    }
}
```