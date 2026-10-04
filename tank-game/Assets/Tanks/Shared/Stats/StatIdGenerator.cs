using Unity.Scripting.LifecycleManagement;

namespace Tanks.Shared.Stats
{
    public static partial class StatIdGenerator
    {
        [AutoStaticsCleanup] private static StatId _lastId;
        [AutoStaticsCleanup] private static readonly object _lock = new();

        public static StatId GenerateNext()
        {
            lock (_lock)
            {
                _lastId = new StatId(_lastId.Value + 1);
                return _lastId;
            }
        }
    }
}
