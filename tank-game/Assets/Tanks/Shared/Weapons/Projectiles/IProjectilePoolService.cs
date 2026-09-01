using System;
using System.Runtime.InteropServices;
using Tanks.Shared.Weapons.Data;
using Unity.Netcode;

namespace Tanks.Shared.Weapons.Projectiles
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct ProjectileId : IEquatable<ProjectileId>, INetworkSerializeByMemcpy
    {
        public ProjectileId(ShotId shotId, ushort value)
        {
            ShotId = shotId;
            Value = value;
        }

        public readonly ShotId ShotId;
        public readonly ushort Value;

        public static bool operator ==(in ProjectileId lhs, in ProjectileId rhs) => lhs.Equals(rhs);
        public static bool operator !=(in ProjectileId lhs, in ProjectileId rhs) => !lhs.Equals(rhs);

        public override int GetHashCode()
        {
            int shotId = ShotId.GetHashCode();
            int value = Value.GetHashCode();
            return shotId ^ ((value << 16) | (value >> 24));
        }

        public override string ToString() => (ShotId, Value).ToString();
        public override bool Equals(object obj) => obj is ProjectileId other && Equals(other);

        public bool Equals(ProjectileId other)
        {
            return ShotId == other.ShotId && Value == other.Value;
        }
    }

    public interface IProjectilePoolService : INetworkBehaviourExtended
    {
        bool TryInitializePool(IProjectileConfig config);
        bool TryGetProjectile(ProjectileId id, out IProjectileBehaviour instance);
        bool TryRent(IProjectileConfig config, ProjectileId id, out IProjectileBehaviour instance);

        void RequestReconciliation(IProjectileBehaviour projectile, ProjectileSnapshotFromServer snapshot);
        void ReturnProjectileClientRpc(ProjectileId id);
        void RejectReturnClientRpc(ProjectileReturnRequestRejected message);
        void RequestReturnAsClient(IProjectileBehaviour instance);

        void Return(IProjectileBehaviour instance, bool removeHandle = true);
        void ReturnAll();
    }
}
