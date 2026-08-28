using System.Collections.Generic;

namespace Tanks.Shared.Entities
{
    public interface IEntityService
    {
        IEntity GetById(EntityId id);
        void AddEntity(IEntity entity);
        void RemoveEntity(IEntity entity);
    }

    public sealed class EntityService : IEntityService
    {
        private readonly Dictionary<EntityId, IEntity> _entities = new();

        public IEntity GetById(EntityId id)
        {
            return _entities.TryGetValue(id, out IEntity result) ? result : null;
        }

        public void AddEntity(IEntity entity)
        {
            _entities.Add(entity.Id, entity);
        }

        public void RemoveEntity(IEntity entity)
        {
            _entities.Remove(entity.Id);
        }
    }
}
