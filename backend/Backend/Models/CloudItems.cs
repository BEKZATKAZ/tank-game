using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.CloudSave.Model;

namespace Backend.Models;

public class CloudItemEntry
{
    public required string InitialJson { get; init; }
    public required string? WriteLock { get; init; }
    public object? Value;

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.None,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new DefaultNamingStrategy()
        }
    };

    public object GetValue()
    {
        if (Value != null) return Value;

        Value = JToken.Parse(InitialJson).ToObject<object>()
            ?? new Dictionary<string, object>();

        return Value;
    }

    public T As<T>(T? fallback = default)
    {
        if (Value is T result) return result;

        T? deserialized = JsonConvert
            .DeserializeObject<T>(InitialJson, JsonSettings) ?? fallback;

        Value = deserialized;
        return deserialized ?? throw new NullReferenceException();
    }
}

public class CloudItems
{
    public CloudItems(List<Item> items)
    {
        _dictionary = items.ToDictionary(k => k.Key, v => new CloudItemEntry
        {
            InitialJson = JsonConvert.SerializeObject(v.Value),
            WriteLock = v.WriteLock
        });
    }

    private readonly Dictionary<string, CloudItemEntry> _dictionary;

    public T Get<T>(string key, T? fallback = default)
    {
        if (_dictionary.TryGetValue(key, out CloudItemEntry? entry) == false)
        {
            if (fallback is null) throw new KeyNotFoundException(key);

            entry = new CloudItemEntry
            {
                InitialJson = "null",
                WriteLock = null!,
                Value = fallback
            };

            _dictionary.Add(key, entry);
        }

        return entry.As(fallback);
    }

    public void Set<T>(string key, T value)
    {
        if (_dictionary.TryGetValue(key, out CloudItemEntry? entry) == false)
        {
            entry = new CloudItemEntry
            {
                InitialJson = "null",
                WriteLock = null!,
                Value = value
            };

            _dictionary.Add(key, entry);
            return;
        }

        entry.Value = value;
    }

    public List<SetItemBody> GetSetters()
    {
        return [.. _dictionary.Select(x =>
            new SetItemBody(x.Key, x.Value.GetValue(), x.Value.WriteLock!))];
    }
}
