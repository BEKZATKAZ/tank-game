using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Entities;
using Tanks.Client.Rendering;
using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Netcode;

namespace Tanks.Client.Player
{
    public interface ISpectatorService
    {
        bool IsSpectating { get; }
        bool CanSwitchTarget { get; }

        void Spectate(Required<IEntityBehaviour> entity);
        void PreviousTarget();
        void NextTarget();
    }

    public interface ISpectatorTargetProvider
    {
        IEnumerable<IEntityBehaviour> GetTargets();
    }

    public sealed class SpectatorTargetProvider : ISpectatorTargetProvider
    {
        public IEnumerable<IEntityBehaviour> GetTargets()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager == null || manager.SpawnManager == null) yield break;

            foreach (NetworkObject player in manager.SpawnManager.PlayerObjects)
            {
                if (player == null || player.IsSpawned == false) continue;
                yield return player.GetComponent<IEntityBehaviour>();
            }
        }
    }

    public sealed class SpectatorService : ISpectatorService
    {
        public SpectatorService(
            IClientEntityService clientEntityService,
            ICameraFollowService followService,
            ISpectatorTargetProvider targetProvider)
        {
            _clientEntityService = clientEntityService;
            _followService = followService;
            _targetProvider = targetProvider;
        }

        private readonly IClientEntityService _clientEntityService;
        private readonly ICameraFollowService _followService;
        private readonly ISpectatorTargetProvider _targetProvider;

        public bool IsSpectating => _clientEntityService.GetPossessedEntity() is
            { IsValid: true, IsLocalPlayer: false };

        public bool CanSwitchTarget => IsSpectating && GetTargets().Skip(1).Any();

        private IEnumerable<IEntityBehaviour> GetTargets()
        {
            return _targetProvider.GetTargets().Where(x =>
                x is { IsValid: true, IsLocalPlayer: false } &&
                x.GetBehaviour<IHealthBehaviour>()?.ServerState.Value.Current > 0);
        }

        public void PreviousTarget() => SwitchTarget(-1);
        public void NextTarget() => SwitchTarget(1);

        private void SwitchTarget(int direction)
        {
            if (IsSpectating == false) return;

            IEntityBehaviour[] targets = GetTargets().OrderBy(x => x.OwnerClientId).ToArray();
            if (targets.Length < 2) return;

            IEntityBehaviour current = _clientEntityService.GetPossessedEntity();
            int index = System.Array.IndexOf(targets, current);
            int next = index < 0
                ? (direction > 0 ? 0 : targets.Length - 1)
                : (index + direction + targets.Length) % targets.Length;

            Spectate(targets[next].AsRequired());
        }

        public void Spectate(Required<IEntityBehaviour> entity)
        {
            _followService.SetFollowTarget(entity.Value.Transform, instant: false);
            _clientEntityService.PossessEntity(entity);
        }
    }
}
