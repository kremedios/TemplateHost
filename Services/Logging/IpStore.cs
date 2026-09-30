
using System.Text.Json;
using System.Text.Json.Serialization;
using AppContractsSCO.Models.Common;

namespace Host.Services.Logging;

public static class IpStore
{
    private static readonly Dictionary<string, IpRecord> _records = new();
    private static readonly object _lock = new();

    public static int Count
    {
        get
        {
            lock (_lock)
            {
                return _records.Count;
            }
        }
    }

    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        string json = File.ReadAllText(filePath);

        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        var records = JsonSerializer.Deserialize<List<IpRecord>>(
            json,
            options);

        if (records == null)
            return;

        lock (_lock)
        {
            _records.Clear();

            foreach (var record in records)
            {
                if (!string.IsNullOrWhiteSpace(record.Ip))
                {
                    _records[record.Ip] = record;
                }
            }
        }
    }

    public static IpRecord? Get(string ip)
    {
        lock (_lock)
        {
            return _records.TryGetValue(ip, out var record)
                ? record
                : null;
        }
    }

    public static void AddOrUpdate(IpRecord record)
    {
        lock (_lock)
        {
            _records[record.Ip] = record;
        }
    }

    public static void Save(string filePath)
    {
        List<IpRecord> records;

        lock (_lock)
        {
            records = _records.Values.ToList();
        }

        string json = JsonSerializer.Serialize(
            records,
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            });

        File.WriteAllText(filePath, json);
    }
}

