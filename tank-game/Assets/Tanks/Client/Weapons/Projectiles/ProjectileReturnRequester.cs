using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;
using UnityEngine;

namespace Tanks.Client.Weapons.Projectiles
{
    public struct PendingProjectileReturn
    {
        public ProjectileId Id;
        public IProjectileConfig Config;
        public IWeaponHolderBehaviour Owner;
    }

    public sealed class ProjectileReturnRequester :
        NetworkComponent<IProjectilePoolService>,
        INetworkMessageHandler<ProjectileReturnRequested>,
        INetworkMessageHandler<ProjectileReturnRequestRejected>
    {
        public IEnumerable<PendingProjectileReturn> Pending => _pending.Values;

        private readonly Dictionary<ProjectileId, PendingProjectileReturn> _pending = new();

        public void Handle(ProjectileReturnRequested message)
        {
            if (Behaviour.TryGetProjectile(message.Id, out IProjectileBehaviour instance) == false)
            {
                return;
            }

            _pending[message.Id] = new PendingProjectileReturn
            {
                Id = message.Id,
                Config = instance.Config,
                Owner = instance.Owner
            };
        }

        public void Handle(ProjectileReturnRequestRejected message)
        {
            if (_pending.Remove(message.Id, out PendingProjectileReturn pending) == false)
            {
                return;
            }

            Debug.LogWarning($"Projectile desync detected for {message.Id}. Syncing...");

            if (Behaviour.TryGetProjectile(message.Id, out IProjectileBehaviour instance) == false)
            {
                Debug.LogWarning("Failed to get projectile instance for desync recovery");
                return;
            }

            instance.Transform.WorldPosition = message.Position;
            instance.Transform.WorldRotation = message.Rotation;
            instance.Launch(pending.Owner, message.Id);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<ProjectileReturnRequested>(this);
            Behaviour.RegisterMessageHandler<ProjectileReturnRequestRejected>(this);
        }
    }
}
