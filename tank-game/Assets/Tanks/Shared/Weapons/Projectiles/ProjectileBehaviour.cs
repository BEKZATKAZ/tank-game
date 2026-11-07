using System.Runtime.InteropServices;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles.Modules;
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
        IWeaponHolderBehaviour Owner { get; }
        string Name { get; }
        float ElapsedSeconds { get; set; }

        ProjectileId Id { get; }
        float Radius { get; }
        float Speed { get; }
        float LifeTime { get; }
        ushort MaxRicochetCount { get; }

        void Launch(IWeaponHolderBehaviour owner, in ProjectileId id);
        void Reconcile(ProjectileSnapshotFromServer snapshot);
        void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime);

        void OnRicochet(float2 hitPoint, float2 surfaceNormal);
        void OnDamageableHit(IHealthBehaviour damageable);
    }

    [RequireComponent(typeof(ProjectileModuleList))]
    public class ProjectileBehaviour :
        NetworkBehaviourExtended, IProjectileBehaviour, IMessageSender
    {
        [SerializeField] private float _radius = 0.5f;
        [SerializeField] private byte _speed = 15;
        [SerializeField] private byte _lifeTime = 10;
        [SerializeField] private byte _maxRicochetCount = 10;

        public IWeaponHolderBehaviour Owner { get; private set; }
        public ProjectileId Id { get; private set; }
        public float ElapsedSeconds { get; set; }

        public string Name => name;
        public float Radius => _radius;
        public float Speed => _speed;
        public float LifeTime => _lifeTime;
        public ushort MaxRicochetCount => _maxRicochetCount;

        private ProjectileMover _mover;
        private ProjectileModuleList _moduleList;

        public void Reconcile(ProjectileSnapshotFromServer snapshot)
        {
            EmitMessage(snapshot);
        }

        public void OnDamageableHit(IHealthBehaviour damageable)
        {
            _moduleList.Call(module => module.HitEntity(damageable.Entity, Owner.Entity));
        }

        public void OnRicochet(float2 hitPoint, float2 surfaceNormal)
        {
            EmitMessage(new ProjectileRicocheted
            {
                HitPoint = hitPoint,
                SurfaceNormal = surfaceNormal
            });

            _moduleList.Call(module => module.HitWall(hitPoint, surfaceNormal));
        }

        public void Launch(IWeaponHolderBehaviour owner, in ProjectileId id)
        {
            Id = id;
            Owner = owner;
            EmitMessage(new ProjectileLaunched { Owner = owner });
        }

        public void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime)
        {
            _mover.Simulate(callbacks, transform, _speed * deltaTime);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Id == default) return;

            _moduleList.Call(module => module.ProjectileDone());
        }

        protected override void Awake()
        {
            base.Awake();
            _moduleList = GetComponent<ProjectileModuleList>();
            _mover = new ProjectileMover(_radius);
            ForceInitialize();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
#endif
    }
}
