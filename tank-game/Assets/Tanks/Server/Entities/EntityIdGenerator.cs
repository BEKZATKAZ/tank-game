using Tanks.Shared.Entities;

namespace Tanks.Server.Entities
{
    public static class EntityIdGenerator
    {
        private static readonly object _lock = new object();
        private static int _lastId;

        public static EntityId GenerateNext()
        {
            lock (_lock) return new EntityId(++_lastId);
        }
    }
}
