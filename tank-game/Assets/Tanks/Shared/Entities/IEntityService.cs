using System.Collections.Generic;

namespace Tanks.Shared.Entities
{
    public interface IEntityService
    {
        IEnumerable<IEntityBehaviour> GetAllEntities();
        IEntityBehaviour GetById(EntityId id);

        void AddEntity(IEntityBehaviour entity);
        void RemoveEntity(IEntityBehaviour entity);
    }

    public sealed class EntityService : IEntityService
    {
        private readonly Dictionary<EntityId, IEntityBehaviour> _entities = new();

        public IEnumerable<IEntityBehaviour> GetAllEntities()
        {
            return _entities.Values;
        }

        public IEntityBehaviour GetById(EntityId id)
        {
            return _entities.TryGetValue(id, out IEntityBehaviour result) ? result : null;
        }

        public void AddEntity(IEntityBehaviour entity)
        {
            _entities.Add(entity.Id, entity);
        }

        public void RemoveEntity(IEntityBehaviour entity)
        {
            _entities.Remove(entity.Id);
        }
    }
}
