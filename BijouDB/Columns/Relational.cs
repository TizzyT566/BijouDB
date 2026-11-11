namespace BijouDB;

// A non-generic registry lets both generic directions meet during circular static initialization.
internal sealed class RelationshipStorage
{
    private static readonly Dictionary<string, RelationshipStorage> _registry = new(StringComparer.Ordinal);
    private static readonly object _registryLock = new();

    private readonly string _directoryName;
    private readonly HashSet<string> _legacyDirectoryNames = new(StringComparer.Ordinal);
    private readonly HashSet<Type> _owners = new();

    internal string Name { get; }

    internal string DirectoryPath => Path.Combine(Globals.DatabasePath, _directoryName);

    private RelationshipStorage(string pair, string name)
    {
        Name = name;
        _directoryName = $"{pair}.relations.{name}";
    }

    internal static RelationshipStorage Get(Type owner, Type target, string legacySuffix, string? name, RelationshipStorage? reciprocal)
    {
        name ??= reciprocal?.Name ?? "default";
        if (name.Length == 0 || name.Length > 80 || name.Any(c => !IsNameCharacter(c)))
            throw new ArgumentException("Relationship names must contain 1 to 80 ASCII letters, digits, periods, underscores, or hyphens.", nameof(name));
        if (reciprocal is not null && !StringComparer.Ordinal.Equals(name, reciprocal.Name))
            throw new ArgumentException("Both ends of a relationship must use the same name.", nameof(name));

        string first = owner.FullName!, second = target.FullName!;
        string pair = StringComparer.Ordinal.Compare(first, second) <= 0 ? $"{first}-{second}" : $"{second}-{first}";
        string key = pair + "|" + name;

        lock (_registryLock)
        {
            if (!_registry.TryGetValue(key, out RelationshipStorage definition))
                _registry.Add(key, definition = new(pair, name));
            if (!definition._owners.Add(owner))
                throw new InvalidOperationException($"A relationship named '{name}' between {owner.FullName} and {target.FullName} is already registered. Use a distinct name for each relationship.");
            // Old counter-based directories are unambiguous only for the default relationship.
            if (name == "default")
            {
                string legacyPair = first.CompareTo(second) < 0 ? $"{first}-{second}" : $"{second}-{first}";
                definition._legacyDirectoryNames.Add(legacyPair + legacySuffix);
            }

            return definition;
        }
    }

    private static bool IsNameCharacter(char c) => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '.';

    internal string[] Directories()
    {
        lock (_registryLock)
            return new[] { DirectoryPath }.Concat(_legacyDirectoryNames.Select(name => Path.Combine(Globals.DatabasePath, name))).Distinct(StringComparer.Ordinal).ToArray();
    }
}

public sealed class Relational<R1, R2>
    where R1 : Record, new()
    where R2 : Record, new()
{
    private readonly RelationshipStorage _storage;

    internal string Name => _storage.Name;

    internal Relational(string legacyColumnName, Relational<R2, R1>? reciprocal = null, string? name = null) => _storage = RelationshipStorage.Get(typeof(R1), typeof(R2), legacyColumnName, name, reciprocal?._storage);

    public Junc To(R1 record)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));
        return new Junc(record, _storage);
    }

    internal void Remove(Record record)
    {
        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            if (record._id is not Guid id) return;
            Storage.RunTransaction(() =>
            {
                foreach (string directory in _storage.Directories())
                    DeleteLinks(directory, id);
            });
        }
    }

    private static bool TryReadEndpoints(string path, out Guid first, out Guid second)
    {
        string name = Path.GetFileName(path);
        first = second = Guid.Empty;
        return name.Length == 73 && name[36] == '.' && Guid.TryParseExact(name.Substring(0, 36), "D", out first) && first != Guid.Empty && Guid.TryParseExact(name.Substring(37, 36), "D", out second) && second != Guid.Empty;
    }

    private static void DeleteLinks(string directory, Guid id)
    {
        if (!Directory.Exists(directory)) return;
        foreach (string file in Directory.GetFiles(directory))
            if (TryReadEndpoints(file, out Guid first, out Guid second) && (first == id || second == id))
                Storage.DeleteFile(file);
    }

    public struct Junc
    {
        private readonly Guid _id;
        private readonly RelationshipStorage? _storage;
        private readonly string? _directory;

        // Kept for source compatibility with callers constructing their own junction directory.
        public Junc(R1 record, string dir)
        {
            if (record is null) throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrEmpty(dir)) throw new ArgumentException("A directory is required.", nameof(dir));

            lock (Storage.SyncRoot)
            {
                Storage.EnsureInitialized();
                _id = record.Id;
                _directory = dir;
                _storage = null;
            }
        }

        internal Junc(R1 record, RelationshipStorage storage)
        {
            lock (Storage.SyncRoot)
            {
                Storage.EnsureInitialized();
                _id = record.Id;
                _storage = storage;
                _directory = null;
            }
        }

        private string WriteDirectory => _storage?.DirectoryPath ?? _directory ?? throw new InvalidOperationException("The junction has not been initialized.");

        private string[] Directories => _storage?.Directories() ?? new[] { WriteDirectory };

        public void Add(params R2[] records)
        {
            if (records is null) throw new ArgumentNullException(nameof(records));

            lock (Storage.SyncRoot)
            {
                Storage.EnsureInitialized();
                string directory = WriteDirectory;
                Guid owner = _id;
                if (!Record.TryGet<R1>(owner, out _)) throw new FileNotFoundException($"Record {owner} does not exist.");

                // Validate all endpoints before creating links; Id persists fresh targets.
                Guid[] ids = records.Select(record => record is null ? throw new ArgumentNullException(nameof(records)) : record.Id).ToArray();
                foreach (Guid id in ids)
                    if (!Record.TryGet<R2>(id, out _)) throw new FileNotFoundException($"Record {id} does not exist.");

                Storage.RunTransaction(() =>
                {
                    foreach (Guid id in ids)
                        Storage.CreateEmptyFile(Path.Combine(directory, ResolveFilename(owner, id)));
                });
            }
        }

        public void Remove(params R2[] records)
        {
            if (records is null) throw new ArgumentNullException(nameof(records));

            lock (Storage.SyncRoot)
            {
                Storage.EnsureInitialized();
                string[] directories = Directories;
                Guid owner = _id;
                foreach (R2 record in records)
                    if (record is null) throw new ArgumentNullException(nameof(records));

                Storage.RunTransaction(() =>
                {
                    foreach (R2 record in records)
                    {
                        if (record._id is not Guid id) continue;
                        foreach (string directory in directories)
                        {
                            string path = Path.Combine(directory, ResolveFilename(owner, id));
                            if (File.Exists(path))
                                Storage.DeleteFile(path);
                        }
                    }
                });
            }
        }

        public void Clear()
        {
            lock (Storage.SyncRoot)
            {
                Storage.EnsureInitialized();
                string[] directories = Directories;
                Guid owner = _id;
                Storage.RunTransaction(() =>
                {
                    foreach (string directory in directories)
                        DeleteLinks(directory, owner);
                });
            }
        }

        public IEnumerable<R2> All
        {
            get
            {
                lock (Storage.SyncRoot)
                {
                    Storage.EnsureInitialized();
                    List<R2> records = new();
                    HashSet<Guid> seen = new();
                    foreach (string directory in Directories)
                    {
                        if (!Directory.Exists(directory)) continue;
                        foreach (string file in Directory.GetFiles(directory))
                        {
                            if (!TryReadEndpoints(file, out Guid first, out Guid second)) continue;

                            Guid other;
                            if (first == _id)
                                other = second;
                            else if (second == _id)
                                other = first;
                            else
                                continue;
                            if (seen.Add(other) && Record.TryGet(other, out R2? record) && record is not null)
                                records.Add(record);
                        }
                    }

                    return records.ToArray();
                }
            }
        }

        public static Junc operator +(Junc junc, R2 record)
        {
            junc.Add(record);
            return junc;
        }

        public static Junc operator -(Junc junc, R2 record)
        {
            junc.Remove(record);
            return junc;
        }

        private static string ResolveFilename(Guid first, Guid second)
        {
            string a = first.ToString("D"), b = second.ToString("D");
            return StringComparer.Ordinal.Compare(a, b) <= 0 ? $"{a}.{b}" : $"{b}.{a}";
        }
    }
}
