using Tanks.Core.Helpers;
using Tanks.Shared.Entities;

namespace Tanks.Client.Entities
{
    public delegate void PossessedEntityChanged(IEntityBehaviour previous, IEntityBehaviour current);

    public interface IClientEntityService
    {
        event PossessedEntityChanged Changed;
        IEntityBehaviour GetPossessedEntity();
        void PossessEntity(Required<IEntityBehaviour> entity);
    }

    public sealed class ClientEntityService : IClientEntityService
    {
        public event PossessedEntityChanged Changed;

        private IEntityBehaviour _possessed;

        public IEntityBehaviour GetPossessedEntity()
        {
            return _possessed;
        }

        public void PossessEntity(Required<IEntityBehaviour> entity)
        {
            IEntityBehaviour previous = _possessed;
            previous?.MarkAsUnpossessed();

            _possessed = entity.Value;
            _possessed.MarkAsPossessed();

            Changed?.Invoke(previous, _possessed);
        }
    }
}
