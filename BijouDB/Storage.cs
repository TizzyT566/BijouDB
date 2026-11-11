using System.Globalization;
using System.Text;

namespace BijouDB;

/// <summary>
/// One process owns a database; all operations within that process share this lock.
/// </summary>
internal static class Storage
{
    internal static readonly object SyncRoot = new();
    internal static readonly StringComparison PathComparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    internal static readonly StringComparer PathComparer = Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal static bool IsInitialized { get; private set; }

    private static FileStream? _processLock;
    private static UndoTransaction? _transaction;
    private static Exception? _fault;

    private static readonly object _schemaLock = new();
    private static readonly Dictionary<Type, string[]> _pendingSchemas = new();

    internal static string MetadataPath => Path.Combine(Globals.DatabasePath, ".bijoudb");

    internal static string RecordPath(Type type, Guid id) => Path.Combine(Globals.DatabasePath, type.FullName!, Globals.Rec, $"{id}.{Globals.Rec}");

    internal static void EnsureInitialized()
    {
        lock (SyncRoot)
        {
            if (_fault is not null) throw new InvalidOperationException("Database recovery failed; restart after resolving the underlying I/O failure.", _fault);
            if (IsInitialized)
            {
                ValidatePendingSchemas();
                return;
            }

            RejectLinkedAncestors(Globals.DatabasePath);
            Directory.CreateDirectory(Globals.DatabasePath);

            try
            {
                ValidatePath(Path.Combine(Globals.DatabasePath, ".bijoudb.lock"));
                ValidatePath(MetadataPath);
                _processLock = new(Path.Combine(Globals.DatabasePath, ".bijoudb.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                Directory.CreateDirectory(MetadataPath);
                string metadata = Path.Combine(MetadataPath, "format.txt");
                string expected = "BijouDB:2\nmask:" + Globals.BitMaskSeed.ToString(CultureInfo.InvariantCulture) + "\n";
                if (File.Exists(metadata))
                {
                    if (File.ReadAllText(metadata) != expected) throw new InvalidDataException("Database format or BitMaskSeed does not match the stored metadata.");
                }
                else
                    WriteTextAtomic(metadata, expected);
                string journals = Path.Combine(MetadataPath, "transactions");
                Directory.CreateDirectory(journals);
                foreach (string directory in Directory.GetDirectories(journals))
                {
                    ValidatePath(directory);
                    if (!File.Exists(Path.Combine(directory, "committed")))
                    {
                        UndoTransaction.Restore(directory);
                        WriteTextAtomic(Path.Combine(directory, "committed"), "recovered\n");
                    }

                    UndoTransaction.CleanupDirectory(directory);
                }

                IsInitialized = true;
                ValidatePendingSchemas();
            }
            catch
            {
                if (!IsInitialized)
                {
                    _processLock?.Dispose();
                    _processLock = null;
                }

                throw;
            }
        }
    }

    // Class initializers declare metadata without acquiring the storage lock. Reads may need to
    // initialize another record type while they hold that lock, so the opposite ordering deadlocks.
    internal static void DeclareSchema(Type type, IReadOnlyList<string> definitions)
    {
        Record.RegisterType(type);

        lock (_schemaLock)
            _pendingSchemas[type] = definitions.ToArray();
    }

    private static void ValidatePendingSchemas()
    {
        KeyValuePair<Type, string[]>[] pending;

        lock (_schemaLock)
            pending = _pendingSchemas.ToArray();
        foreach (KeyValuePair<Type, string[]> schema in pending)
        {
            ValidateSchema(schema.Key, schema.Value);

            lock (_schemaLock)
                if (_pendingSchemas.TryGetValue(schema.Key, out string[]? current) && ReferenceEquals(current, schema.Value))
                    _pendingSchemas.Remove(schema.Key);
        }
    }

    internal static void RegisterSchema(Type type, IReadOnlyList<string> definitions)
    {
        Record.RegisterType(type);

        lock (SyncRoot)
        {
            EnsureInitialized();
            ValidateSchema(type, definitions);
        }
    }

    private static void ValidateSchema(Type type, IReadOnlyList<string> definitions)
    {
        string directory = Path.Combine(MetadataPath, "schemas");
        Directory.CreateDirectory(directory);
        string filename = Convert.ToBase64String(Encoding.UTF8.GetBytes(type.FullName!)).Replace('/', '_').Replace('+', '-');
        string path = Path.Combine(directory, filename + ".txt");
        string schema = type.FullName + "\n" + string.Join("\n", definitions) + "\n";
        if (File.Exists(path))
        {
            if (File.ReadAllText(path) != schema)
                throw new InvalidDataException($"The stored schema for {type.FullName} differs from this application. Migrate the database before changing column order or types.");
        }
        else
            WriteTextAtomic(path, schema);
    }

    internal static void RunTransaction(Action action)
    {
        lock (SyncRoot)
        {
            EnsureInitialized();
            if (_transaction is not null)
            {
                action();
                return;
            }

            UndoTransaction transaction = new();
            _transaction = transaction;

            try
            {
                action();
                transaction.Commit();
            }
            catch (Exception original)
            {
                try
                {
                    transaction.Rollback();
                }
                catch (Exception rollback)
                {
                    _fault = new AggregateException(original, rollback);
                    throw new InvalidOperationException("The operation failed and its rollback needs recovery before further access.", _fault);
                }

                throw;
            }
            finally
            {
                _transaction = null;
                // Cleanup diagnostics may invoke a user logger, which may itself access the database.
                if (transaction.IsResolved)
                    transaction.Cleanup();
            }
        }
    }

    internal static void RemoveRecord(Record record, IReadOnlyList<Action<Record>> columns, IReadOnlyList<Func<Record, bool>> references)
    {
        lock (SyncRoot)
        {
            EnsureInitialized();
            if (!record._id.HasValue || !File.Exists(RecordPath(record.GetType(), record._id.Value))) throw new FileNotFoundException("The record does not exist.");
            foreach (Func<Record, bool> check in references)
                if (check(record)) throw new InvalidOperationException("The record cannot be removed while restricted child references exist.");

            RunTransaction(() =>
            {
                foreach (Action<Record> remove in columns)
                    remove(record);

                DeleteFile(RecordPath(record.GetType(), record._id.Value));
            });
        }
    }

    internal static void WriteFile(string path, Stream source)
    {
        ValidatePath(path);
        _transaction?.Capture(path);
        WriteAtomic(path, source);
    }

    internal static void CreateEmptyFile(string path)
    {
        using MemoryStream empty = new();
        WriteFile(path, empty);
    }

    internal static void DeleteFile(string path)
    {
        ValidatePath(path);
        if (!File.Exists(path)) return;
        _transaction?.Capture(path);
        File.Delete(path);
    }

    internal static void DeleteDirectory(string path)
    {
        ValidatePath(path);
        if (!Directory.Exists(path)) return;
        foreach (string file in Directory.GetFiles(path))
            DeleteFile(file);

        foreach (string directory in Directory.GetDirectories(path))
            DeleteDirectory(directory);

        Directory.Delete(path);
    }

    internal static void WriteTextAtomic(string path, string text)
    {
        using MemoryStream source = new(new UTF8Encoding(false).GetBytes(text), false);
        WriteAtomic(path, source);
    }

    internal static void WriteAtomic(string path, Stream source)
    {
        ValidatePath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = Path.Combine(Path.GetDirectoryName(path)!, ".bijou-" + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            using (FileStream output = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                source.CopyTo(output);
                output.Flush(true);
            }

            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }
        finally
        {
            if (File.Exists(temporary))
                File.Delete(temporary);
        }
    }

    internal static void ValidatePath(string path)
    {
        string full = Path.GetFullPath(path);
        string root = Path.GetFullPath(Globals.DatabasePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, PathComparison)) throw new InvalidDataException("Storage path is outside the database directory.");
        RejectLinkedAncestors(full);
    }

    private static void RejectLinkedAncestors(string path)
    {
        for (string? current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException($"Storage does not follow linked directories or files: {current}");
    }

    private sealed class UndoTransaction
    {
        private readonly string _directory;
        private readonly HashSet<string> _captured = new(PathComparer);

        internal bool IsResolved { get; private set; }

        internal UndoTransaction()
        {
            _directory = Path.Combine(MetadataPath, "transactions", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        internal void Capture(string path)
        {
            path = Path.GetFullPath(path);
            ValidatePath(path);
            string root = Path.GetFullPath(Globals.DatabasePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(root, PathComparison)) throw new InvalidOperationException("Transaction path is outside the database directory.");
            if (_captured.Contains(path)) return;

            string backup = _captured.Count.ToString(CultureInfo.InvariantCulture) + ".bak";
            bool exists = File.Exists(path);
            if (exists)
            {
                File.Copy(path, Path.Combine(_directory, backup));
                using FileStream stream = new(Path.Combine(_directory, backup), FileMode.Open, FileAccess.Write, FileShare.None);
                stream.Flush(true);
            }

            string relative = path.Substring(root.Length);
            string entry = Convert.ToBase64String(Encoding.UTF8.GetBytes(relative)) + "\t" + (exists ? backup : "-") + "\n";
            using (FileStream log = new(Path.Combine(_directory, "entries.log"), FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(entry);
                log.Write(bytes, 0, bytes.Length);
                log.Flush(true);
            }

            _captured.Add(path);
        }

        internal void Commit()
        {
            WriteTextAtomic(Path.Combine(_directory, "committed"), "committed\n");
            IsResolved = true;
        }

        internal void Rollback()
        {
            Restore(_directory);
            // Cleanup may fail. A resolved marker prevents this undo from replaying over later commits.
            WriteTextAtomic(Path.Combine(_directory, "committed"), "rolled-back\n");
            IsResolved = true;
        }

        internal void Cleanup() => CleanupDirectory(_directory);

        internal static void CleanupDirectory(string directory)
        {
            try
            {
                string marker = Path.Combine(directory, "committed");
                // Keep the terminal marker until all undo data is gone, even when a backup is locked.
                foreach (string file in Directory.GetFiles(directory))
                    if (!string.Equals(file, marker, PathComparison))
                    {
                        ValidatePath(file);
                        File.Delete(file);
                    }

                ValidatePath(marker);
                File.Delete(marker);
                Directory.Delete(directory);
            }
            catch (Exception ex)
            {
                ex.Log(Globals.LogLevel.Warning);
            } // Committed/rolled-back journal is safe to retry next open.
        }

        internal static void Restore(string directory)
        {
            string manifest = Path.Combine(directory, "entries.log");
            if (!File.Exists(manifest)) return;

            string text = File.ReadAllText(manifest);
            // An incomplete final append cannot have been followed by a mutation.
            int complete = text.LastIndexOf('\n');
            if (complete < 0) return;

            string root = Path.GetFullPath(Globals.DatabasePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            foreach (string line in text.Substring(0, complete).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Reverse())
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 2) throw new InvalidDataException("Invalid transaction journal entry.");

                string path = Path.GetFullPath(Path.Combine(root, Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]))));
                if (!path.StartsWith(root, PathComparison)) throw new InvalidDataException("Transaction journal path escapes the database directory.");
                ValidatePath(path);
                if (parts[1] == "-")
                {
                    if (File.Exists(path))
                        File.Delete(path);
                }
                else
                {
                    if (Path.GetFileName(parts[1]) != parts[1]) throw new InvalidDataException("Invalid transaction backup filename.");

                    string backupPath = Path.Combine(directory, parts[1]);
                    ValidatePath(backupPath);
                    if (File.Exists(path))
                    {
                        bool identical;
                        using (FileStream current = File.OpenRead(path))
                        using (FileStream saved = File.OpenRead(backupPath))
                            identical = Misc.StreamCompare(current, saved);
                        // A failed replacement/deletion may have left the original locked file unchanged.
                        if (identical) continue;
                    }

                    using FileStream backup = File.OpenRead(backupPath);
                    WriteAtomic(path, backup);
                }
            }
        }
    }
}
