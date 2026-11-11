using System.Globalization;
using System.Runtime.CompilerServices;

namespace BijouDB;

/// <summary>
/// Database maintenance operations. Configure Globals before the first access.
/// </summary>
public static class Database
{
    /// <summary>
    /// Check stored column pointers and index references. Repair reconstructs references, never missing values.
    /// </summary>
    public static IntegrityReport CheckIntegrity(bool repair = false)
    {
        // Schema declarations are initialized outside the storage lock to avoid class-initializer lock inversion.
        string root = Path.GetFullPath(Globals.DatabasePath);
        if (Directory.Exists(root))
            foreach (string directory in Directory.GetDirectories(root))
            {
                try
                {
                    Storage.ValidatePath(Path.Combine(directory, Globals.Rec));
                }
                catch (InvalidDataException)
                {
                    continue;
                }

                if (Directory.Exists(Path.Combine(directory, Globals.Rec)) && Record.TryResolveType(Path.GetFileName(directory), out Type? type))
                    RuntimeHelpers.RunClassConstructor(type!.TypeHandle);
            }

        lock (Storage.SyncRoot)
        {
            Storage.EnsureInitialized();
            List<string> issues = new();
            List<Action> fixes = new();
            HashSet<string> knownTypes = new(StringComparer.Ordinal);
            string schemas = Path.Combine(Storage.MetadataPath, "schemas");
            if (Directory.Exists(schemas))
                foreach (string manifest in Directory.GetFiles(schemas, "*.txt"))
                    InspectSchema(manifest, root, knownTypes, issues, fixes);
            foreach (string directory in Directory.GetDirectories(root))
                if (Directory.Exists(Path.Combine(directory, Globals.Rec)) && !knownTypes.Contains(Path.GetFileName(directory)))
                    issues.Add($"No registered schema for record directory: {directory}");

            if (!repair || fixes.Count == 0) return new IntegrityReport(issues, 0);
            Storage.RunTransaction(() =>
            {
                foreach (Action fix in fixes)
                    fix();
            });
            IntegrityReport remaining = CheckIntegrity();
            return new IntegrityReport(remaining.Issues, fixes.Count);
        }
    }

    private static void InspectSchema(string manifest, string root, HashSet<string> knownTypes, List<string> issues, List<Action> fixes)
    {
        string[] lines = File.ReadAllLines(manifest);
        if (lines.Length == 0 || string.IsNullOrWhiteSpace(lines[0]))
        {
            issues.Add($"Invalid schema manifest: {manifest}");
            return;
        }

        string type = lines[0];
        string typeDirectory = Path.GetFullPath(Path.Combine(root, type));
        string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!typeDirectory.StartsWith(prefix, Storage.PathComparison) || Path.GetFileName(typeDirectory) != type)
        {
            issues.Add($"Invalid schema record path: {manifest}");
            return;
        }

        knownTypes.Add(type);
        if (!SafePath(Path.Combine(typeDirectory, Globals.Rec), issues)) return;

        List<CheckedColumn> columns = new();
        HashSet<int> ordinals = new();
        foreach (string line in lines.Skip(1).Where(line => !string.IsNullOrEmpty(line)))
        {
            string[] parts = line.Split('|');
            if (parts.Length < 3 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int ordinal) || !ordinals.Add(ordinal))
            {
                issues.Add($"Invalid or duplicate schema ordinal: {manifest}");
                return;
            }

            if (parts[0] == "reference" || parts[0] == "relationship") continue;
            if (parts[0] != "column" || parts.Length != 6 || !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long offset) || offset % 24 != 0)
            {
                issues.Add($"Invalid column descriptor: {manifest}");
                return;
            }

            columns.Add(new CheckedColumn(Path.Combine(typeDirectory, Globals.Index, Globals.ColName + "_" + ordinal), offset));
        }

        if (!ordinals.OrderBy(value => value)
            .SequenceEqual(Enumerable.Range(0, ordinals.Count)) || !columns.Select(column => column.Offset)
            .OrderBy(offset => offset)
            .SequenceEqual(Enumerable.Range(0, columns.Count)
            .Select(index => (long)index * 24)))
        {
            issues.Add($"Invalid or duplicate schema offsets: {manifest}");
            return;
        }

        foreach (CheckedColumn column in columns)
            if (!SafePath(column.Directory, issues))
                column.Complete = false;

        string records = Path.Combine(typeDirectory, Globals.Rec);
        if (Directory.Exists(records))
            foreach (string recordPath in Directory.GetFiles(records, Globals.RecPattern))
            {
                if (!SafePath(recordPath, issues))
                {
                    foreach (CheckedColumn column in columns)
                        column.Complete = false;

                    continue;
                }

                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(recordPath), out Guid id) || id == Guid.Empty)
                {
                    issues.Add($"Invalid record filename: {recordPath}");
                    foreach (CheckedColumn column in columns)
                        column.Complete = false;

                    continue;
                }

                using FileStream record = File.OpenRead(recordPath);
                if (record.Length % 24 != 0 || (columns.Count == 0 ? record.Length != 0 : record.Length > columns.Max(column => column.Offset) + 24))
                {
                    issues.Add($"Record length does not match its schema: {recordPath}");
                    foreach (CheckedColumn column in columns)
                        column.Complete = false;

                    continue;
                }

                foreach (CheckedColumn column in columns)
                {
                    if (record.Length <= column.Offset) continue;
                    record.Position = column.Offset;
                    if (!record.ReadHashValue(out ulong hash, out Guid index) || (index == Guid.Empty && hash != 0))
                    {
                        issues.Add($"Invalid column pointer: {recordPath} at {column.Offset}");
                        column.Complete = false;
                        continue;
                    }

                    if (index == Guid.Empty) continue;

                    string directory = Path.Combine(column.Directory, hash.PaddedString(), index.ToString());
                    string reference = Path.Combine(directory, id + "." + Globals.Ref);
                    column.Expected.Add(reference);
                    string value = Path.Combine(directory, Globals.BinFile);
                    if (!SafePath(value, issues))
                    {
                        column.Complete = false;
                        continue;
                    }

                    if (!File.Exists(value))
                    {
                        issues.Add($"Missing stored value: {value}");
                        column.Complete = false;
                        continue;
                    }

                    using (FileStream data = File.OpenRead(value))
                        if (data.GetSkipHash() != hash)
                        {
                            issues.Add($"Stored value hash mismatch: {value}");
                            column.Complete = false;
                        }

                    if (!File.Exists(reference))
                    {
                        issues.Add($"Missing index reference: {reference}");
                        fixes.Add(() => Storage.CreateEmptyFile(reference));
                    }
                }
            }

        foreach (CheckedColumn column in columns)
        {
            if (!SafePath(column.Directory, issues)) continue;
            if (!Directory.Exists(column.Directory)) continue;
            foreach (string hashDirectory in Directory.GetDirectories(column.Directory))
            {
                if (!SafePath(hashDirectory, issues)) continue;
                if (!ulong.TryParse(Path.GetFileName(hashDirectory), NumberStyles.None, CultureInfo.InvariantCulture, out _))
                {
                    issues.Add($"Invalid hash directory: {hashDirectory}");
                    continue;
                }

                foreach (string valueDirectory in Directory.GetDirectories(hashDirectory))
                {
                    if (!SafePath(valueDirectory, issues)) continue;
                    if (!Guid.TryParse(Path.GetFileName(valueDirectory), out _))
                    {
                        issues.Add($"Invalid value directory: {valueDirectory}");
                        continue;
                    }

                    string[] files = Directory.GetFiles(valueDirectory);
                    if (files.Any(file => !SafePath(file, issues)))
                    {
                        column.Complete = false;
                        continue;
                    }

                    foreach (string file in files.Where(file => Path.GetExtension(file) == "." + Globals.Ref))
                        if (!column.Expected.Contains(file))
                        {
                            issues.Add($"Stale index reference: {file}");
                            if (column.Complete)
                                fixes.Add(() => Storage.DeleteFile(file));
                        }

                    bool hasRecordPointer = column.Expected.Any(path => string.Equals(Path.GetDirectoryName(path), valueDirectory, Storage.PathComparison));
                    if (!hasRecordPointer)
                    {
                        issues.Add($"Unreferenced indexed value: {valueDirectory}");
                        if (column.Complete && Directory.GetDirectories(valueDirectory).Length == 0 && files.All(file => Path.GetFileName(file) == Globals.BinFile || Path.GetExtension(file) == "." + Globals.Ref))
                            fixes.Add(() => Storage.DeleteDirectory(valueDirectory));
                    }
                }
            }
        }
    }

    private static bool SafePath(string path, List<string> issues)
    {
        try
        {
            Storage.ValidatePath(path);
            return true;
        }
        catch (InvalidDataException error)
        {
            issues.Add(error.Message);
            return false;
        }
    }

    private sealed class CheckedColumn
    {
        internal string Directory { get; }

        internal long Offset { get; }

        internal bool Complete = true;

        internal HashSet<string> Expected { get; } = new(Storage.PathComparer);

        internal CheckedColumn(string directory, long offset)
        {
            Directory = directory;
            Offset = offset;
        }
    }
}

public sealed class IntegrityReport
{
    public IReadOnlyList<string> Issues { get; }

    public int RepairedCount { get; }

    public bool IsValid => Issues.Count == 0;

    internal IntegrityReport(IEnumerable<string> issues, int repairedCount)
    {
        Issues = Array.AsReadOnly(issues.ToArray());
        RepairedCount = repairedCount;
    }
}
