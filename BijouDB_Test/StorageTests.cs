using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using BijouDB;
using BijouDB.Exceptions;

namespace BijouDB_Test;

internal static class StorageTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("failed constraints preserve existing values and indexes", () =>
        {
            UniqueTestRow a = new()
            {
                Value = 1
            };
            UniqueTestRow b = new()
            {
                Value = 2
            };
            TestAssert.Throws<UniqueConstraintException<@int>>(() => a.Value = 2);
            TestAssert.Equal(1, a.Value);
            TestAssert.Equal(a.Id, UniqueTestRow.ValueColumn.WithValue<UniqueTestRow>(1).Single().Id);
            TestAssert.Throws<CheckContraintException>(() => a.Value = -1);
            TestAssert.Equal(1, a.Value);
            a.Value = 1;
            TestAssert.Equal(1, a.Value);
        });
        test("colliding values are all found and same-hash updates remove old references", () =>
        {
            byte[] aValue = new byte[4000], bValue = new byte[4000];
            bValue[32] = 1;
            CollisionTestRow a = new()
            {
                Value = aValue
            };
            CollisionTestRow b = new()
            {
                Value = bValue
            };
            byte[] aRecord = File.ReadAllBytes(Storage.RecordPath(typeof(CollisionTestRow), a.Id));
            byte[] bRecord = File.ReadAllBytes(Storage.RecordPath(typeof(CollisionTestRow), b.Id));
            TestAssert.Equal(BitConverter.ToUInt64(aRecord), BitConverter.ToUInt64(bRecord));
            TestAssert.Equal(b.Id, CollisionTestRow.ValueColumn.WithValue<CollisionTestRow>(bValue).Single().Id);
            a.Value = bValue;
            TestAssert.Equal(0, CollisionTestRow.ValueColumn.WithValue<CollisionTestRow>(aValue).Count());
            TestAssert.Equal(2, CollisionTestRow.ValueColumn.WithValue<CollisionTestRow>(bValue).Count());
            TestAssert.True(a.TryRemove());
            TestAssert.True(b.TryRemove());
        });
        test("concurrent inserts enforce unique values", () =>
        {
            UniqueTestRow[] rows = Enumerable.Range(0, 16).Select(_ => new UniqueTestRow()).ToArray();
            foreach (UniqueTestRow row in rows)
                _ = row.Id;

            int accepted = 0;
            Parallel.ForEach(rows, row =>
            {
                try
                {
                    row.Value = 123456;
                    Interlocked.Increment(ref accepted);
                }
                catch (UniqueConstraintException<@int>)
                {
                }
            });
            TestAssert.Equal(1, accepted);
            TestAssert.Equal(1, UniqueTestRow.ValueColumn.WithValue<UniqueTestRow>(123456).Count());
        });
        test("large indexed strings compare and can be removed immediately after lookup", () =>
        {
            string value = new('x', 20000);
            TextTestRow row = new()
            {
                Value = value
            };
            for (int i = 0; i < 10; i++)
                TestAssert.Equal(1, TextTestRow.ValueColumn.WithValue<TextTestRow>(value).Count());

            TestAssert.True(row.TryRemove(out _));
            TestAssert.True(!Record.TryGet<TextTestRow>(row.Id, out _));
            TestAssert.Throws<FileNotFoundException>(() => _ = row.Value);
        });
        test("transaction failure rolls back changed and deleted files", () =>
        {
            string path = Path.Combine(Globals.DatabasePath, "rollback", "original.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "old");
            string added = Path.Combine(Globals.DatabasePath, "rollback", "added.txt");
            TestAssert.Throws<InvalidOperationException>(() => Storage.RunTransaction(() =>
            {
                using MemoryStream changed = new(Encoding.UTF8.GetBytes("new"));
                Storage.WriteFile(path, changed);
                Storage.CreateEmptyFile(added);
                throw new InvalidOperationException("injected failure");
            }));
            TestAssert.Equal("old", File.ReadAllText(path));
            TestAssert.True(!File.Exists(added));
            TestAssert.Throws<InvalidOperationException>(() => Storage.RunTransaction(() =>
            {
                Storage.DeleteFile(path);
                throw new InvalidOperationException();
            }));
            TestAssert.Equal("old", File.ReadAllText(path));
        });
        test("configuration freezes and schema mismatches are rejected", () =>
        {
            Storage.EnsureInitialized();
            TestAssert.Throws<InvalidOperationException>(() => Globals.DatabasePath = Path.Combine(Globals.DatabasePath, "other"));
            TestAssert.Throws<InvalidOperationException>(() => Globals.BitMaskSeed++);
            _ = new UniqueTestRow().Id;
            TestAssert.Throws<InvalidDataException>(() => Storage.RegisterSchema(typeof(UniqueTestRow), new[] { "different" }));
        });
        test("record types in another assembly are discovered", () =>
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("BijouExternalModels"), AssemblyBuilderAccess.Run);
            TypeBuilder builder = assembly.DefineDynamicModule("models").DefineType("ExternalModels.Row", TypeAttributes.Public, typeof(Record));
            builder.DefineDefaultConstructor(MethodAttributes.Public);
            Type type = builder.CreateType()!;
            Record.RegisterAssembly(assembly);
            TestAssert.True(Record.Types.Contains(type.FullName!));
            TestAssert.True(Record.TryResolveType(type.FullName!, out Type? resolved));
            TestAssert.Equal(type, resolved);
        });
        test("invalid IDs and absent intersection operands return no matches", () =>
        {
            TestAssert.True(!Record.TryGet<UniqueTestRow>("bad-guid", out _));
            TestAssert.True(!Record.TryGet<UniqueTestRow>(Guid.Empty, out _));
            TestAssert.Equal(0, Record.WithValues<UniqueTestRow>(null!, null!).Count());
        });
        test("missing stored values report corruption", () =>
        {
            TextTestRow row = new()
            {
                Value = "missing fixture"
            };
            byte[] bytes = File.ReadAllBytes(Storage.RecordPath(typeof(TextTestRow), row.Id));
            string bin = Path.Combine(Globals.DatabasePath, typeof(TextTestRow).FullName!, "indx", "col_0", BitConverter.ToUInt64(bytes)
                .PaddedString(), new Guid(bytes.Skip(8)
                .Take(16)
                .ToArray()).ToString(), "value.bin");
            File.Delete(bin);
            TestAssert.Throws<InvalidDataException>(() => _ = row.Value);
            // Restore fixture so later integrity checks are meaningful.
            @string value = "missing fixture";
            using BijouDB.Components.FileBackedStream serialized = new();
            value.Hash(serialized);
            serialized.Position = 0;
            Storage.WriteAtomic(bin, serialized);
        });
        test("database survives reopening in another process", () =>
        {
            string path = ChildPath("reopen");
            TestAssert.Equal(0, Child("seed", path));
            TestAssert.Equal(0, Child("read", path));
        });
        test("database rejects a second owning process", () => TestAssert.Equal(0, Child("locked", Globals.DatabasePath)));
        test("record ID collisions retry a new filename", () => TestAssert.Equal(0, Child("id-collision", ChildPath("collision"))));
        test("interrupted writes recover from undo journals on reopen", () =>
        {
            string path = ChildPath("recovery");
            TestAssert.Equal(23, Child("interrupt", path));
            TestAssert.Equal(0, Child("recover", path));
        });
        test("rolled-back staged directories do not poison later writes", () =>
        {
            byte[] payload = new byte[345];
            @blob value = payload;
            using BijouDB.Components.FileBackedStream serialized = new();
            ulong hash = value.Hash(serialized);
            string directory = Path.Combine(Globals.DatabasePath, typeof(CollisionTestRow).FullName!, "indx", "col_0", hash.PaddedString(), Guid.NewGuid()
                .ToString());
            TestAssert.Throws<InvalidOperationException>(() => Storage.RunTransaction(() =>
            {
                serialized.Position = 0;
                Storage.WriteFile(Path.Combine(directory, "value.bin"), serialized);
                throw new InvalidOperationException("injected staged failure");
            }));
            CollisionTestRow row = new()
            {
                Value = payload
            };
            TestAssert.Equal(row.Id, CollisionTestRow.ValueColumn.WithValue<CollisionTestRow>(payload).Single().Id);
            row.Remove();
        });
        test("journal cleanup failures never undo later commits", () =>
        {
            foreach (string mode in new[] { "cleanup-commit", "cleanup-rollback" })
            {
                string path = ChildPath(mode);
                TestAssert.Equal(0, Child(mode, path));
                TestAssert.Equal(0, Child("cleanup-read", path));
            }
        });
        test("integrity repair rebuilds missing references and removes stale references", () =>
        {
            TextTestRow row = new()
            {
                Value = "integrity fixture"
            };
            byte[] bytes = File.ReadAllBytes(Storage.RecordPath(typeof(TextTestRow), row.Id));
            string directory = Path.Combine(Globals.DatabasePath, typeof(TextTestRow).FullName!, "indx", "col_0", BitConverter.ToUInt64(bytes)
                .PaddedString(), new Guid(bytes.Skip(8)
                .Take(16)
                .ToArray()).ToString());
            string reference = Path.Combine(directory, row.Id + ".ref");
            File.Delete(reference);
            string stale = Path.Combine(directory, Guid.NewGuid() + ".ref");
            File.WriteAllBytes(stale, Array.Empty<byte>());
            TestAssert.True(!Database.CheckIntegrity().IsValid);
            IntegrityReport repaired = Database.CheckIntegrity(true);
            TestAssert.True(repaired.RepairedCount >= 2);
            TestAssert.True(repaired.IsValid, string.Join(Environment.NewLine, repaired.Issues));
            TestAssert.True(File.Exists(reference));
            TestAssert.True(!File.Exists(stale));
            TestAssert.Equal(row.Id, TextTestRow.ValueColumn.WithValue<TextTestRow>("integrity fixture").Single().Id);
        });
        test("integrity repair reports missing values without fabricating data", () =>
        {
            TextTestRow row = new()
            {
                Value = "integrity missing value"
            };
            byte[] bytes = File.ReadAllBytes(Storage.RecordPath(typeof(TextTestRow), row.Id));
            string bin = Path.Combine(Globals.DatabasePath, typeof(TextTestRow).FullName!, "indx", "col_0", BitConverter.ToUInt64(bytes)
                .PaddedString(), new Guid(bytes.Skip(8)
                .Take(16)
                .ToArray()).ToString(), "value.bin");
            byte[] original = File.ReadAllBytes(bin);
            File.Delete(bin);
            TestAssert.True(!Database.CheckIntegrity(true).IsValid);
            TestAssert.True(!File.Exists(bin));
            File.WriteAllBytes(bin, original);
            TestAssert.True(Database.CheckIntegrity().IsValid);
        });
        test("integrity repair rejects duplicate schema offsets and ordinals", () =>
        {
            string type = "MalformedMetadata.Row";
            string directory = Path.Combine(Globals.DatabasePath, type);
            string records = Path.Combine(directory, "rcrd");
            Directory.CreateDirectory(records);
            string manifest = Path.Combine(Storage.MetadataPath, "schemas", "malformed-fixture.txt");
            Guid id = Guid.NewGuid(), first = Guid.NewGuid(), second = Guid.NewGuid();
            string firstDirectory = Path.Combine(directory, "indx", "col_0", 1UL.PaddedString(), first.ToString());
            string secondDirectory = Path.Combine(directory, "indx", "col_0", 2UL.PaddedString(), second.ToString());
            Directory.CreateDirectory(firstDirectory);
            Directory.CreateDirectory(secondDirectory);
            foreach (string valueDirectory in new[] { firstDirectory, secondDirectory })
            {
                File.WriteAllText(Path.Combine(valueDirectory, "value.bin"), "must survive");
                File.WriteAllBytes(Path.Combine(valueDirectory, id + ".ref"), Array.Empty<byte>());
            }

            using (FileStream record = File.Create(Path.Combine(records, id + ".rcrd")))
            {
                record.WriteHashValue(1, first);
                record.WriteHashValue(2, second);
            }

            try
            {
                foreach (string descriptor in new[] { "column|0|Second|24|BijouDB.int|unique=False", "column|1|Second|0|BijouDB.int|unique=False" })
                {
                    File.WriteAllText(manifest, type + "\ncolumn|0|First|0|BijouDB.int|unique=False\n" + descriptor + "\n");
                    IntegrityReport report = Database.CheckIntegrity(true);
                    TestAssert.True(!report.IsValid);
                    TestAssert.True(File.Exists(Path.Combine(firstDirectory, "value.bin")));
                    TestAssert.True(File.Exists(Path.Combine(secondDirectory, "value.bin")));
                    TestAssert.True(File.Exists(Path.Combine(firstDirectory, id + ".ref")));
                    TestAssert.True(File.Exists(Path.Combine(secondDirectory, id + ".ref")));
                }
            }
            finally
            {
                Storage.RunTransaction(() =>
                {
                    Storage.DeleteDirectory(directory);
                    Storage.DeleteFile(manifest);
                });
            }
        });
        test("repair and mutation reject links pointing outside the database", () =>
        {
            string outside = ChildPath("external-files");
            Directory.CreateDirectory(outside);
            string protectedFile = Path.Combine(outside, "value.bin");
            File.WriteAllText(protectedFile, "external data");
            string link = Path.Combine(Globals.DatabasePath, typeof(TextTestRow).FullName!, "indx", "col_0", 12345UL.PaddedString(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.GetDirectoryName(link)!);

            try
            {
                try
                {
                    Directory.CreateSymbolicLink(link, outside);
                }
                catch (Exception error) when (OperatingSystem.IsWindows() && (error is UnauthorizedAccessException || (error is IOException && (error.HResult & 0xffff) == 1314)))
                {
                    ProcessStartInfo info = new("cmd.exe")
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    foreach (string argument in new[] { "/c", "mklink", "/J", link, outside })
                        info.ArgumentList.Add(argument);

                    using Process process = Process.Start(info)!;
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill(true);
                        throw new TimeoutException("Junction fixture creation timed out.");
                    }

                    TestAssert.Equal(0, process.ExitCode);
                }

                IntegrityReport report = Database.CheckIntegrity(true);
                TestAssert.True(!report.IsValid);
                TestAssert.Equal("external data", File.ReadAllText(protectedFile));
                TestAssert.Throws<InvalidDataException>(() => Storage.RunTransaction(() => Storage.DeleteFile(Path.Combine(link, "value.bin"))));
                TestAssert.Equal("external data", File.ReadAllText(protectedFile));
            }
            finally
            {
                if (Directory.Exists(link))
                    Directory.Delete(link);
                File.Delete(protectedFile);
                Directory.Delete(outside);
            }

            TestAssert.True(Database.CheckIntegrity().IsValid);
        });
    }

    private static string ChildPath(string name) => Path.Combine(Path.GetTempPath(), "bijoudb-tests-" + name + "-" + Guid.NewGuid().ToString("N"));

    private static int Child(string mode, string database)
    {
        ProcessStartInfo info = new("dotnet")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        info.ArgumentList.Add("--child");
        info.ArgumentList.Add(mode);
        info.ArgumentList.Add(database);
        using Process process = Process.Start(info)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(), stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(15000))
        {
            process.Kill(true);
            process.WaitForExit();
            throw new TimeoutException($"Child {mode} did not finish.");
        }

        string output = stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult();
        if (process.ExitCode != 0 && mode != "interrupt") throw new InvalidOperationException($"Child {mode} failed: {output}");
        return process.ExitCode;
    }

    internal static int RunChild(string mode, string path)
    {
        Globals.DatabasePath = path;
        if (mode == "locked")
        {
            TestAssert.Throws<IOException>(Storage.EnsureInitialized);
            return 0;
        }

        if (mode == "id-collision")
        {
            string directory = Path.Combine(path, typeof(UniqueTestRow).FullName!, "rcrd");
            Directory.CreateDirectory(directory);
            Guid collision = new("00000001-0000-0000-0000-000000000000");
            File.WriteAllBytes(Path.Combine(directory, collision + ".rcrd"), Array.Empty<byte>());
            TestAssert.True(new UniqueTestRow().Id != collision);
            return 0;
        }

        if (mode == "seed")
        {
            PersistenceTestRow row = new()
            {
                Value = 42,
                Bytes = Enumerable.Range(0, 1048580).Select(i => (byte)i).ToArray()
            };
            TestAssert.True(row.Id != Guid.Empty);
            return 0;
        }

        if (mode == "read")
        {
            PersistenceTestRow row = Record.GetAll<PersistenceTestRow>().Single();
            TestAssert.Equal(42, row.Value);
            TestAssert.Equal(1048580, row.Bytes.Length);
            TestAssert.Equal((byte)255, row.Bytes[255]);
            TestAssert.Equal(row.Id, PersistenceTestRow.ValueColumn.WithValue<PersistenceTestRow>(42).Single().Id);
            return 0;
        }

        Storage.EnsureInitialized();
        if (mode.StartsWith("cleanup-", StringComparison.Ordinal))
        {
            string file = Path.Combine(path, "cleanup.txt");
            if (mode == "cleanup-read")
            {
                TestAssert.Equal("latest", File.ReadAllText(file));
                return 0;
            }

            File.WriteAllText(file, "old");
            FileStream? blocker = null;
            void Change()
            {
                using MemoryStream changed = new(Encoding.UTF8.GetBytes("intermediate"));
                Storage.WriteFile(file, changed);
                string journal = Directory.GetDirectories(Path.Combine(Storage.MetadataPath, "transactions")).Single();
                blocker = new(Directory.GetFiles(journal, "*.bak").Single(), FileMode.Open, FileAccess.Read, FileShare.Read);
                if (mode == "cleanup-rollback") throw new InvalidOperationException("injected failure");
            }

            try
            {
                if (mode == "cleanup-rollback")
                    TestAssert.Throws<InvalidOperationException>(() => Storage.RunTransaction(Change));
                else
                    Storage.RunTransaction(Change);
                if (OperatingSystem.IsWindows())
                    foreach (string journal in Directory.GetDirectories(Path.Combine(Storage.MetadataPath, "transactions")))
                        TestAssert.True(File.Exists(Path.Combine(journal, "committed")), "Cleanup removed the finalization marker.");
            }
            finally
            {
                blocker?.Dispose();
            }

            Storage.RunTransaction(() =>
            {
                using MemoryStream data = new(Encoding.UTF8.GetBytes("latest"));
                Storage.WriteFile(file, data);
            });
            return 0;
        }

        string original = Path.Combine(path, "interrupted.txt"), added = Path.Combine(path, "interrupted-added.txt");
        if (mode == "interrupt")
        {
            File.WriteAllText(original, "old");
            Storage.RunTransaction(() =>
            {
                using MemoryStream changed = new(Encoding.UTF8.GetBytes("new"));
                Storage.WriteFile(original, changed);
                Storage.CreateEmptyFile(added);
                Environment.Exit(23);
            });
            return 1;
        }

        if (mode == "recover")
        {
            TestAssert.Equal("old", File.ReadAllText(original));
            TestAssert.True(!File.Exists(added));
            TestAssert.Equal(0, Directory.GetDirectories(Path.Combine(Storage.MetadataPath, "transactions")).Length);
            return 0;
        }

        throw new ArgumentException("Unknown child mode.");
    }
}

public sealed class UniqueTestRow : Record
{
    public static readonly Column<@int> ValueColumn;

    static UniqueTestRow() => _ = ~SchemaBuilder<UniqueTestRow>.Add(out ValueColumn, Unique: true, Check: value => value >= 0, Cache: 0);

    public int Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }
}

public sealed class CollisionTestRow : Record
{
    public static readonly Column<@blob> ValueColumn;

    static CollisionTestRow() => _ = ~SchemaBuilder<CollisionTestRow>.Add(out ValueColumn, Cache: 0);

    public byte[] Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }
}

public sealed class TextTestRow : Record
{
    public static readonly Column<@string> ValueColumn;

    static TextTestRow() => _ = ~SchemaBuilder<TextTestRow>.Add(out ValueColumn, Cache: 0);

    public string Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }
}

public sealed class PersistenceTestRow : Record
{
    public static readonly Column<@int> ValueColumn;
    public static readonly Column<@blob> BlobColumn;

    static PersistenceTestRow() => _ = ~SchemaBuilder<PersistenceTestRow>.Add(out ValueColumn, Unique: true).Add(out BlobColumn, Cache: 0);

    public int Value { get => ValueColumn.Get(this); set => ValueColumn.Set(this, value); }

    public byte[] Bytes { get => BlobColumn.Get(this); set => BlobColumn.Set(this, value); }
}
