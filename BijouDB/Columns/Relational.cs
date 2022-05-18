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
        Directory.CreateDirectory(_dir);
    }

    public Junc To(R1 record) => new(record, _dir, _leads);

    internal void Remove(Record record)
    {
        if (!Directory.Exists(_dir)) return;
        string[] files = Directory.GetFiles(_dir, _leads ? $"{record.Id}.*" : $"*.{record.Id}");
        foreach (string file in files) File.Delete(file);
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
            Directory.CreateDirectory(_dir);
            foreach (R2 record in records)
                File.Create(Path.Combine(_dir, _leads ? $"{_id}.{record.Id}" : $"{record.Id}.{_id}")).Dispose();
        }

        public void Remove(params R2[] records)
        {
            foreach (R2 record in records)
            {
                string path = Path.Combine(_dir, _leads ? $"{_id}.{record.Id}" : $"{record.Id}.{_id}");
                if (File.Exists(path)) File.Delete(path);
            }
        }

        public R2[] All
        {
            get
            {
                if (!Directory.Exists(_dir)) return Array.Empty<R2>();
                List<R2> records = new();
                string[] files = Directory.GetFiles(_dir, _leads ? $"{_id}.*" : $"*.{_id}");
                foreach (string file in files)
                {
                    string id = _leads ? Path.GetExtension(file)[1..] : Path.GetFileNameWithoutExtension(file);
                    if (Record.TryGet(id, out R2? r) && r is not null) records.Add(r);
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
