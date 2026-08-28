using Tanks.Shared.Entities;

namespace Tanks.Client.Entities
{
    public interface IClientEntityService
    {
        IEntity GetLocalPlayer();
        void SetLocalPlayer(IEntity entity);
    }

    public sealed class ClientEntityService : IClientEntityService
    {
        private IEntity _localPlayer;

        public IEntity GetLocalPlayer() => _localPlayer;
        public void SetLocalPlayer(IEntity entity) => _localPlayer = entity;
    }
}
