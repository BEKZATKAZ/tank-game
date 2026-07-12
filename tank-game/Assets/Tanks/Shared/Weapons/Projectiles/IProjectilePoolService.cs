using System;
using System.Collections.Generic;
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

        public override int GetHashCode() => HashCode.Combine(ShotId, Value);
        public override string ToString() => (ShotId, Value).ToString();
        public override bool Equals(object obj) => obj is ProjectileId other && Equals(other);

        public bool Equals(ProjectileId other)
        {
            return ShotId == other.ShotId && Value == other.Value;
        }
    }

    public interface IProjectilePoolService
    {
        IEnumerable<KeyValuePair<ProjectileId, IProjectile>> ActiveProjectiles { get; }

        void RequestReconciliation(IProjectile projectile, ProjectileSnapshotFromServer snapshot);
        bool TryInitializePool(IProjectileConfig config);

        bool TryRent(IProjectileConfig config, ProjectileId id, out IProjectile instance);
        void Return(IProjectile instance);
    }
}
