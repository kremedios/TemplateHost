using System.Text.Json;
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
        string json = File.ReadAllText(filePath);

        var records = JsonSerializer.Deserialize<List<IpRecord>>(json);

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
}