using Tanks.Core.Helpers;
using Tanks.Shared.Entities;

namespace Tanks.Client.Entities
{
    public delegate void PossessedPlayerChanged(IEntityBehaviour previous, IEntityBehaviour current);

    public interface IClientEntityService
    {
        event PossessedPlayerChanged Changed;
        IEntityBehaviour GetPossessedPlayer();
        void PossessPlayer(Required<IEntityBehaviour> player);
    }

    public sealed class ClientEntityService : IClientEntityService
    {
        public event PossessedPlayerChanged Changed;

        private IEntityBehaviour _possessed;

        public IEntityBehaviour GetPossessedPlayer()
        {
            return _possessed;
        }

        public void PossessPlayer(Required<IEntityBehaviour> player)
        {
            IEntityBehaviour previous = _possessed;
            _possessed = player.Value;
            Changed?.Invoke(previous, _possessed);
        }
    }
}
