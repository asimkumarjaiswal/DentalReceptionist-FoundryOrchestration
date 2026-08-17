using System.Text.Json;

namespace VoiceDentalReceptionist.Storage;

/// <summary>
/// Minimal JSON-file-backed list storage. Reads/writes the whole file each time —
/// deliberately simple for a learning project. A SemaphoreSlim guards against
/// two calls racing on the same file within a single process run.
/// </summary>
public class JsonStorage<T>
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonStorage(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<List<T>> LoadAllAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                return new List<T>();
            }

            var json = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task AppendAsync(T item)
    {
        await _lock.WaitAsync();
        try
        {
            List<T> items;
            if (File.Exists(_filePath))
            {
                var json = await File.ReadAllTextAsync(_filePath);
                items = string.IsNullOrWhiteSpace(json)
                    ? new List<T>()
                    : JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            else
            {
                items = new List<T>();
            }

            items.Add(item);
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(items, SerializerOptions));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAllAsync(List<T> items)
    {
        await _lock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(_filePath, JsonSerializer.Serialize(items, SerializerOptions));
        }
        finally
        {
            _lock.Release();
        }
    }
}
