using Tanks.Shared.Entities;
using Unity.Scripting.LifecycleManagement;

namespace Tanks.Server.Entities
{
    public static partial class EntityIdGenerator
    {
        [AutoStaticsCleanup] private static readonly object _lock = new object();
        [AutoStaticsCleanup] private static int _lastId;

        public static EntityId GenerateNext()
        {
            lock (_lock) return new EntityId(++_lastId);
        }
    }
}
