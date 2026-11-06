using Unity.Scripting.LifecycleManagement;

namespace Tanks.Shared.Effects
{
    public static partial class EffectIdGenerator
    {
        [AutoStaticsCleanup] private static EffectId _lastId;
        private static readonly object _lock = new();

        public static EffectId GenerateNext()
        {
            lock (_lock)
            {
                _lastId = new EffectId(_lastId.Value + 1);
                return _lastId;
            }
        }
    }
}
