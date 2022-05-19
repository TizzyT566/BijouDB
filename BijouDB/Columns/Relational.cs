namespace BijouDB;

public sealed class Relational<R1, R2> where R1 : Record, new() where R2 : Record, new()
{
    private readonly string _dir;
    private readonly bool _leads;

    internal Relational()
    {
        string r1 = typeof(R1).FullName;
        string[] types = { r1, typeof(R2).FullName };
        Array.Sort(types);
        _leads = r1 == types[0];
        _dir = Path.Combine(Globals.DatabasePath, string.Join("-", types));
    }

    public Junc To(R1 record) => new(record, _dir, _leads);

    internal void Remove(Record record)
    {
        if (!Directory.Exists(_dir)) return;
        string[] files;
        try
        {
            files = Directory.GetFiles(_dir, _leads ? $"{record.Id}.*" : $"*.{record.Id}");
        }
        catch (Exception ex)
        {
            ex.Log();
            return;
        }
        foreach (string file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }
    }

    public struct Junc
    {
        private readonly string _id, _dir;
        private readonly bool _leads;

        public Junc(R1 record, string dir, bool leads)
        {
            _id = record.Id.ToString();
            _dir = dir;
            _leads = leads;
        }

        public void Add(params R2[] records)
        {
            try
            {
                Directory.CreateDirectory(_dir);
            }
            catch (Exception ex)
            {
                ex.Log();
                return;
            }
            foreach (R2 record in records)
            {
                try
                {
                    File.Create(Path.Combine(_dir, _leads ? $"{_id}.{record.Id}" : $"{record.Id}.{_id}")).Dispose();
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        public void Remove(params R2[] records)
        {
            foreach (R2 record in records)
            {
                try
                {
                    File.Delete(Path.Combine(_dir, _leads ? $"{_id}.{record.Id}" : $"{record.Id}.{_id}"));
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        public void Clear()
        {
            string[] files;
            try
            {
                files = Directory.GetFiles(_dir, _leads ? $"{_id}.*" : $"*.{_id}");
            }
            catch (Exception ex)
            {
                ex.Log();
                return;
            }
            foreach (string path in files)
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        public R2[] All
        {
            get
            {
                if (!Directory.Exists(_dir)) return Array.Empty<R2>();
                List<R2> records = new();
                string[] files;
                try
                {
                    files = Directory.GetFiles(_dir, _leads ? $"{_id}.*" : $"*.{_id}");
                }
                catch (Exception ex)
                {
                    ex.Log();
                    return Array.Empty<R2>();
                }
                foreach (string file in files)
                {
                    try
                    {

                        string id = _leads ? Path.GetExtension(file)[1..] : Path.GetFileNameWithoutExtension(file);
                        if (Record.TryGet(id, out R2? r) && r is not null) records.Add(r);
                    }
                    catch (Exception ex)
                    {
                        ex.Log();
                    }
                }
                return records.ToArray();
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
    }
}
