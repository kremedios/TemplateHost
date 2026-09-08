using System.Text.Json;
using AppContractsSCO.Models.Common;

namespace Host.Services.Logging;
/**
IpStore remains responsible for maintaining uniqueness.

IpStore stores all records of IP addresses that have hit the website.
Each IP in IpStore is unique.
*/
public static class IpStore
{
    private static readonly Dictionary<string, IpRecord> _records = new();
    private static readonly object _lock = new();

    //temp
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






   public static IpRecord Add(string ip, string country)
    {
        lock (_lock)
        {
            if (_records.TryGetValue(ip, out var existingRecord))
                return existingRecord;

            var newRecord = new IpRecord
            {
                Ip = ip,
                Country = country,
                AccessIsBlocked = null,
                HitIsToBeRegistered = null
            };

            _records.Add(ip, newRecord);

            return newRecord;
        }
    }

    public static bool Contains(string ip)
    {
        lock (_lock)
        {
            return _records.ContainsKey(ip);
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
}