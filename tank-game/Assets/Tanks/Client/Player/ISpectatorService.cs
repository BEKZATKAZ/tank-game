using Tanks.Client.Entities;
using Tanks.Client.Rendering;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;

namespace Tanks.Client.Player
{
    public interface ISpectatorService
    {
        void Spectate(Required<IEntityBehaviour> entity);
    }

    public sealed class SpectatorService : ISpectatorService
    {
        public SpectatorService(
            IClientEntityService clientEntityService,
            ICameraFollowService followService)
        {
            _clientEntityService = clientEntityService;
            _followService = followService;
        }

        private readonly IClientEntityService _clientEntityService;
        private readonly ICameraFollowService _followService;

        public void Spectate(Required<IEntityBehaviour> entity)
        {
            _followService.SetFollowTarget(entity.Value.Transform, instant: false);
            _clientEntityService.PossessEntity(entity);
        }
    }
}
