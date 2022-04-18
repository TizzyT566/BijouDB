namespace BijouDB.Components;

public class PathBuilder
{
    private readonly List<string> _path;
    private string? _crntPath;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private PathBuilder() { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public PathBuilder(string initialPath)
    {
        _path = Path.GetFullPath(initialPath).Split('\\', StringSplitOptions.RemoveEmptyEntries).ToList();
        _crntPath ??= $@"{_path[0]}\\{string.Join('\\', _path.Skip(1))}\";
    }

    public void Append(string path, bool create = false)
    {
        _path.AddRange(path.Split('\\', StringSplitOptions.RemoveEmptyEntries));
        _crntPath ??= $@"{_path[0]}\\{string.Join('\\', _path.Skip(1))}\";
        if (create) Directory.CreateDirectory(_crntPath);
    }

    public void Back()
    {
        if (_path.Count > 0)
            _path.RemoveAt(_path.Count - 1);
    }

    public override string ToString() => _crntPath ??= $@"{_path[0]}\\{string.Join('\\', _path.Skip(1))}\";
    public string ToString(string fileName) => $"{ToString()}{fileName}";

    public static implicit operator PathBuilder(string path) => new(path);
    public static implicit operator string(PathBuilder pb) => pb.ToString();
}
