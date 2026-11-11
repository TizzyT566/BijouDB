# BijouDB

A small, file-backed C# database with typed columns, indexed values, constraints, record references, and many-to-many relationships.

The library targets .NET Standard 2.0. The regression executable uses .NET 10. Package: [BijouDB on NuGet](https://www.nuget.org/packages/BijouDB/).

This checkout includes storage correctness fixes and new storage metadata. Read [storage behavior and compatibility](docs/STORAGE.md) before opening an existing database or upgrading an application.

The [fix and verification checklist](docs/FIXES.md) maps all 22 review findings to their implementations and regression coverage.

## Getting started

Set the database directory and, if required, the mask seed before the first data access:

```csharp
using BijouDB;

Globals.DatabasePath = Path.Combine(AppContext.BaseDirectory, "DB");
Globals.BitMaskSeed = 712247; // Default; this is obfuscation, not encryption.

var person = new Person { Name = "Ada", Age = 30 };
Console.WriteLine(person.Json);
```

Record classes must be publicly visible and have a public parameterless constructor to satisfy the generic APIs. Retrieval does not execute that constructor.

```csharp
using BijouDB;

public sealed class Person : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Column<@int> AgeColumn;

    [Json] public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }

    [Json] public int Age
    { get => AgeColumn.Get(this); set => AgeColumn.Set(this, value); }

    static Person() => _ = ~SchemaBuilder<Person>
        .Add(out NameColumn, Unique: true)
        .Add(out AgeColumn, Check: age => age >= 18, Default: () => 18);
}
```

The `~` operator disposes the builder and declares the schema. Stored schema manifests are validated on data access. Keep column declarations and their order stable once data exists; the manifests reject subsequent incompatible changes and do not perform migrations.

## Datatypes and constraints

| C# value | BijouDB wrapper |
| --- | --- |
| `BigInteger` | `@bint` |
| `byte[]` | `@blob` |
| `bool`, `byte`, `sbyte`, `char` | `@bool`, `@byte`, `@sbyte`, `@char` |
| `short`, `ushort`, `int`, `uint` | `@short`, `@ushort`, `@int`, `@uint` |
| `long`, `ulong`, `float`, `double`, `decimal` | `@long`, `@ulong`, `@float`, `@double`, `@decimal` |
| `string`, `DateTime` | `@string`, `@time` |
| A record `R` | `@record<R>` |
| Value tuples with two through eight components | `@tuple<D1, D2, ...>` |

Each wrapper also has a nested `.nullable` wrapper. Tuple component types are BijouDB datatypes, including nullable wrappers and custom datatypes. Blob conversions return independent arrays so caller edits do not change cached values.

`SchemaBuilder.Add` accepts these optional column arguments:

- `Unique`: rejects a value already set on another record in that column.
- `Default`: computes the value returned for an unset column.
- `Check`: validates explicitly assigned values.
- `Cache`: maximum cached entries, default `128`; use `0` to disable caching.

Only explicitly assigned values are indexed. Defaults are returned without running `Check` and do not appear in value lookups until assigned. A record is persisted when its `Id` is first accessed, including through a column or relationship operation. Constructing an unused object alone does not persist it.

Implement `IDataType.Serialize(Stream)` and `IDataType.Deserialize(Stream)` to add a custom datatype. Read complete values, reject corrupt input, and use a stable encoding. Built-in dynamic values such as strings and blobs are limited to 256 MiB of encoded payload.

## Finding and removing records

```csharp
if (Record.TryGet<Person>(savedId, out var retrieved))
    Console.WriteLine(retrieved!.Name);

IEnumerable<Person> all = Record.GetAll<Person>();
IEnumerable<Person> named = Person.NameColumn.WithValue<Person>("Ada");
IEnumerable<Person> matches = Record.WithValues(
    Person.NameColumn.WithValue<Person>("Ada"),
    Person.AgeColumn.WithValue<Person>(30));

foreach (int age in Person.AgeColumn.UniqueValues())
    Console.WriteLine(age);
```

`TryGet` accepts a `Guid` or its string representation; invalid IDs return `false`. Storage and schema errors remain observable. Collection lookups return snapshots of matching handles; later property reads access current values.

```csharp
person.Remove(); // Throws if restricted children exist or deletion fails.

if (!person.TryRemove(out Exception? error))
    Console.Error.WriteLine(error!.Message);
```

`TryRemove()` is also available when the error is not needed. It returns `true` only after successful deletion. Removing a record also removes its indexed values and relationship links as appropriate.

Records in loaded class libraries are discoverable through `Record.Types`. `Record.RegisterAssembly(typeof(Person).Assembly)` explicitly registers an assembly when needed.

## One-to-many references

A child stores a record value in a column. The parent's reference queries that column. Restricted references prevent deletion while children still refer to the parent.

```csharp
public sealed class Department : Record
{
    public static readonly Reference<@record<Department>, Worker> WorkerReferences;
    public IEnumerable<Worker> Workers => WorkerReferences.To(this);

    static Department() => _ = ~SchemaBuilder<Department>
        .Add(out WorkerReferences, () => Worker.DepartmentColumn);
}

public sealed class Worker : Record
{
    public static readonly Column<@record<Department>> DepartmentColumn;
    public Department Department
    { get => DepartmentColumn.Get(this); set => DepartmentColumn.Set(this, value); }

    static Worker() => _ = ~SchemaBuilder<Worker>.Add(out DepartmentColumn);
}
```

Pass `restricted: false` to the reference's `Add` call to permit parent deletion. These references are a lightweight relationship mechanism; they do not implement all SQL foreign-key semantics.

## Many-to-many relationships

Give both directions the same relationship name. Their schema positions can differ. Use distinct names for different relationships between the same record types.

```csharp
public sealed class Employee : Record
{
    public static readonly Column<@string> NameColumn;
    public static readonly Relational<Employee, Computer> ComputerRelational;

    public string Name
    { get => NameColumn.Get(this); set => NameColumn.Set(this, value); }
    public Relational<Employee, Computer>.Junc Computers
    { get => ComputerRelational.To(this); set { } }

    static Employee() => _ = ~SchemaBuilder<Employee>
        .Add(out NameColumn)
        .Add(out ComputerRelational, "computers");
}

public sealed class Computer : Record
{
    public static readonly Relational<Computer, Employee> EmployeeRelational;
    public Relational<Computer, Employee>.Junc Employees
    { get => EmployeeRelational.To(this); set { } }

    static Computer() => _ = ~SchemaBuilder<Computer>
        .Add(out EmployeeRelational, "computers");
}

// In application code:
// employee.Computers += computer;
// computer.Employees.Remove(employee);
// employee.Computers.Clear();
// foreach (Computer item in employee.Computers.All) { ... }
```

The reciprocal-field overload remains supported, for example `.Add(out ComputerRelational, Computer.EmployeeRelational)`. Named initialization avoids circular field access. Fresh targets receive persistent IDs when linked. Deleted targets cannot be linked again.

## JSON

Mark record properties with `[Json]` to include them in `record.Json` or `record.ToJson(depth: 2, level: 0)`. Use `[TupleObject("Area", "Exchange", "Subscriber")]` to label tuple components. Depth limits record traversal; the level limits attributed properties. Cyclic record references are abbreviated.

Built-in output escapes strings and characters and formats numbers independently of the current culture. `BigInteger` is a JSON number, `decimal` retains its string representation, `DateTime` is represented as ticks, and `byte[]` is base64. A JSON consumer can still lose precision when it parses large numbers into floating point. NaN and infinity throw rather than produce invalid JSON.

`Json.GetRecord(typeName, id, depth, level)` and `Json.GetProperty(typeName, id, propertyName, depth, level)` read existing records without running their constructors. Missing records return null. Serializer errors, failing getters, and invalid storage propagate.

Add a custom formatter with `Json.TryAddFormatter(type, formatter)`. The callback must return a complete, valid JSON value. For example:

```csharp
Json.TryAddFormatter(typeof(TimeSpan), value =>
    ((TimeSpan)value).Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture));
```

## Storage, logging, and integrity

One process owns the configured database directory. Threads in that process are serialized for storage operations. The directory and `Globals.BitMaskSeed` freeze at the first data access; pure schema declaration does not freeze configuration. Masking is deterministic XOR obfuscation. It does not provide confidentiality, authentication, or protection from malicious file contents.

Set `Globals.Logging = true` for console error output, or assign `Globals.Logger` to receive exceptions and log levels through your own logger.

`Database.CheckIntegrity()` inspects record pointers and indexes. `Database.CheckIntegrity(repair: true)` can repair safe reference/index inconsistencies; it cannot recover missing binary values. Back up an existing database before migration or repair. See [STORAGE.md](docs/STORAGE.md) for the format, recovery limits, and upgrade details.

Lookup cost includes serialization, hashing, equality comparisons, filesystem operations, and result enumeration. ID retrieval addresses a known file; value lookup checks a hash bucket and enumerates its matching references. Relationship queries scan link directories. No constant-time or logarithmic filesystem performance guarantee is made.

## Building and testing

Install the .NET 10 SDK, then run from the repository directory:

```text
dotnet build BijouDB.sln --configuration Release
dotnet run --project BijouDB_Test --configuration Release
```

`BijouDB_Test` is a regression console executable. It reports assertions and exits with a nonzero code if any test fails. It needs no external test-framework packages. Child-process cases cover reopening and bounded recovery/hang scenarios. CI builds and runs the suite on Windows and Linux.
