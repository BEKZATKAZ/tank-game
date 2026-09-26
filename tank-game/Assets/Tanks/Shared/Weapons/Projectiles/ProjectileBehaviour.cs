using System.Runtime.InteropServices;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Tanks.Shared.Weapons.Projectiles
{
    public struct ProjectileRicocheted
    {
        public float2 HitPoint;
        public float2 SurfaceNormal;
    }

    public struct ProjectileLaunched
    {
        public IDamage Damage;
        public IWeaponHolderBehaviour Owner;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProjectileSnapshotFromServer : INetworkSerializeByMemcpy
    {
        public float2 ServerPosition;
        public Angle ServerRotation;
        public int ServerTick;
        public int ServerRicochetedCount;
        public float ServerElapsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProjectileLaunchSettings : INetworkSerializeByMemcpy
    {
        public float Speed;
        public float LifeTime;
        public ushort MaxRicochetCount;
    }

    public interface IProjectileBehaviour : INetworkBehaviourExtended
    {
        IDamage Damage { get; }
        IWeaponHolderBehaviour Owner { get; }
        IProjectileConfig Config { get; }

        ProjectileId Id { get; }
        float Radius { get; }
        float ElapsedSeconds { get; set; }

        void Launch(IWeaponHolderBehaviour owner, in ProjectileId id);
        void Initialize(Required<IProjectileConfig> config, Required<IDamage> damage);

        void Reconcile(ProjectileSnapshotFromServer snapshot);
        void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime);
        void OnRicochet(float2 hitPoint, float2 surfaceNormal);
    }

    public class ProjectileBehaviour :
        NetworkBehaviourExtended, IProjectileBehaviour, IMessageSender
    {
        [SerializeField] private float _radius = 0.5f;

        public IProjectileConfig Config { get; private set; }
        public IDamage Damage { get; private set; }
        public IWeaponHolderBehaviour Owner { get; private set; }

        public ProjectileId Id { get; private set; }
        public float ElapsedSeconds { get; set; }
        public float Radius => _radius;

        private ProjectileMover _mover;

        public void Reconcile(ProjectileSnapshotFromServer snapshot)
        {
            EmitMessage(snapshot);
        }

        public void OnRicochet(float2 hitPoint, float2 surfaceNormal)
        {
            EmitMessage(new ProjectileRicocheted
            {
                HitPoint = hitPoint,
                SurfaceNormal = surfaceNormal
            });
        }

        public void Initialize(Required<IProjectileConfig> config, Required<IDamage> damage)
        {
            Config = config.Value;
            Damage = damage.Value;
        }

        public void Launch(IWeaponHolderBehaviour owner, in ProjectileId id)
        {
            Id = id;
            Owner = owner;
            EmitMessage(new ProjectileLaunched { Damage = Damage, Owner = owner });
        }

        public void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime)
        {
            _mover.Simulate(callbacks, transform, Config.Speed * deltaTime);
        }

        protected override void Awake()
        {
            base.Awake();
            _mover = new ProjectileMover(_radius);
            ForceInitialize();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
