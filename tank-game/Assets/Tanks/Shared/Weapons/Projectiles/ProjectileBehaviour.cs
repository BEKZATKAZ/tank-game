using System.Runtime.InteropServices;
using System.Linq;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;
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
        IEntityBehaviour DirectHitTarget { get; }

        void Launch(IWeaponHolderBehaviour owner, in ProjectileId id);
        void Reconcile(ProjectileSnapshotFromServer snapshot);
        void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime);

        void OnRicochet(float2 hitPoint, float2 surfaceNormal);
        void OnDamageableHit(IHealthBehaviour damageable);
        void OnDamageableHit(IHealthBehaviour damageable, float2 impactPosition, float2 incomingDirection);
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
        public IEntityBehaviour DirectHitTarget { get; private set; }
        public float DirectHitDamageMultiplier { get; private set; } = 1;
        public float2 ImpactPosition { get; private set; }

        public string Name => name;
        public float Radius => _radius;
        public float Speed => _speed;
        public float LifeTime => _lifeTime;
        public ushort MaxRicochetCount => _maxRicochetCount;

        private ProjectileMover _mover;
        private ProjectileModuleList _moduleList;
        private bool _hasHitTarget;
        private float2 _incomingDirection;

        public void Reconcile(ProjectileSnapshotFromServer snapshot)
        {
            EmitMessage(snapshot);
        }

        public void OnDamageableHit(IHealthBehaviour damageable)
        {
            if (_hasHitTarget) return;
            _hasHitTarget = true;
            DirectHitTarget = damageable.Entity;
            DirectHitDamageMultiplier = 1;

            if (damageable.Entity != Owner.Entity && damageable.ServerState.Value.Current > 0 &&
                damageable.Entity.GetBehaviour<ITeamParticipantBehaviour>()?.State != null)
            {
                IItemOwnerBehaviour items = Owner.Entity.GetBehaviour<IItemOwnerBehaviour>();
                DirectHitContext context = new DirectHitContext
                {
                    ProjectileId = Id,
                    Target = damageable.Entity,
                    Direction = _incomingDirection,
                    DamageMultiplier = 1
                };

                if (items != null)
                {
                    foreach (ItemBehaviourBase behaviour in items.GetAllBehaviours().Distinct())
                    {
                        try { behaviour.OnDirectHit(items, ref context); }
                        catch (System.Exception ex) { Debug.LogException(ex); }
                    }
                }

                DirectHitDamageMultiplier = context.DamageMultiplier;
            }

            _moduleList.Call(module => module.HitEntity(damageable.Entity, Owner.Entity));
        }

        public void OnDamageableHit(IHealthBehaviour damageable, float2 impactPosition, float2 incomingDirection)
        {
            ImpactPosition = impactPosition;
            _incomingDirection = incomingDirection;
            OnDamageableHit(damageable);
        }

        public void OnRicochet(float2 hitPoint, float2 surfaceNormal)
        {
            ImpactPosition = hitPoint;
            if (MaxRicochetCount == 0) Transform.WorldPosition = hitPoint + surfaceNormal * 0.01f;
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
            _hasHitTarget = false;
            DirectHitTarget = null;
            DirectHitDamageMultiplier = 1;
            ImpactPosition = Transform.WorldPosition;
            _incomingDirection = Transform.Right;
            EmitMessage(new ProjectileLaunched { Owner = owner });
        }

        public void Simulate(LaunchCallbacks callbacks, ITransform transform, float deltaTime)
        {
            _incomingDirection = transform.Right;
            _mover.Simulate(new LaunchCallbacks
            {
                OnDamageableHit = target =>
                {
                    ImpactPosition = transform.WorldPosition;
                    callbacks.OnDamageableHit?.Invoke(target);
                },
                OnRicochet = callbacks.OnRicochet
            }, transform, _speed * deltaTime);
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
