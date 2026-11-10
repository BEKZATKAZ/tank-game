using Unity.Services.CloudSave.Model;

namespace Backend.Tests.Helpers;

internal static class CloudSaveItems
{
    public static Item Create(string key, object value, string writeLock)
    {
        ModifiedMetadata timestamp = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        return new Item(key, value, writeLock, timestamp, timestamp);
    }
}
