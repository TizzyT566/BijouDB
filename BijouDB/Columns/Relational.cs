namespace BijouDB;

public sealed class Relational<R1, R2> where R1 : Record, new() where R2 : Record, new()
{
    private readonly string _dir;

    internal Relational(string colName)
    {
        string r1 = typeof(R1).FullName;
        string r2 = typeof(R2).FullName;

        _dir = r1.CompareTo(r2) < 0 ?
            Path.Combine(Globals.DatabasePath, $"{r1}-{r2}{colName}") :
            Path.Combine(Globals.DatabasePath, $"{r2}-{r1}{colName}");
    }

    public Junc To(R1 record) => new(record, _dir);

    internal void Remove(Record record)
    {
        if (!Directory.Exists(_dir))
            new Exception("Record doesn't exist.").Log(Globals.LogLevel.Warning);
        else
            try
            {
                foreach (string file in Directory.EnumerateFiles(_dir, $"*{record.Id}*"))
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
    }

    public struct Junc
    {
        private readonly string _id, _dir;

        public Junc(R1 record, string dir)
        {
            _id = record.Id.ToString();
            _dir = dir;
        }

        public void Add(params R2[] records)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                foreach (R2 record in records)
                    try
                    {
                        string path = Path.Combine(_dir, ResolveFilename(_id, record._id.ToString()));
                        if (!File.Exists(path))
                            File.Create(path).Dispose();
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        public void Remove(params R2[] records)
        {
            foreach (R2 record in records)
                try
                {
                    File.Delete(Path.Combine(_dir, ResolveFilename(_id, record._id.ToString())));
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
        }

        public void Clear()
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(_dir, $"*{_id}*"))
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        public IEnumerable<R2> All
        {
            get
            {
                if (!Directory.Exists(_dir)) yield break;

                foreach (string file in Directory.EnumerateFiles(_dir, $"*{_id}*"))
                    if (file.EndsWith(_id) && Record.TryGet(Path.GetFileNameWithoutExtension(file), out R2? rEnd) && rEnd is not null) yield return rEnd;
                    else if (Record.TryGet(Path.GetExtension(file).Substring(1), out R2? rBegin) && rBegin is not null) yield return rBegin;
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

        private static string ResolveFilename(string id1, string id2) => id1.CompareTo(id2) < 0 ? $"{id1}.{id2}" : $"{id2}.{id1}";
    }
}
