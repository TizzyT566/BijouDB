namespace BijouDB;

public sealed class Relational<R1, R2> where R1 : Record, new() where R2 : Record, new()
{
    private readonly string _dir;
    private readonly bool _leads;

    internal Relational(Func<Relational<R2, R1>> _)
    {
        string r1 = typeof(R1).FullName;
        string[] types = { r1, typeof(R2).FullName };
        Array.Sort(types);
        _leads = r1 == types[0];
        _dir = Path.Combine(Globals.DatabasePath, string.Join("-", types));
        Directory.CreateDirectory(_dir);
    }

    public Junc To(R1 record) => new(record, this);

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

        public Junc(R1 record, Relational<R1, R2> relational)
        {
            _id = record.Id.ToString();
            _dir = relational._dir;
            _leads = relational._leads;
        }

        public void Add(params R2[] records)
        {
            foreach (R2 record in records)
            {
                Directory.CreateDirectory(_dir);
                File.Create(Path.Combine(_dir, _leads ? $"{_id}.{record.Id}" : $"{record.Id}.{_id}")).Dispose();
            }
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
                List<R2> records = new();
                if (!Directory.Exists(_dir)) return records.ToArray();
                string[] files = Directory.GetFiles(_dir, _leads ? $"{_id}.*" : $"*.{_id}");
                foreach (string file in files)
                {
                    string id = _leads ? Path.GetExtension(file).Substring(1) : Path.GetFileNameWithoutExtension(file);
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
