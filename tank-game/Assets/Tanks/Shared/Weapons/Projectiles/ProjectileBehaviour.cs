using System.Runtime.InteropServices;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Data;
using Unity.Mathematics;
using Unity.Netcode;

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
        public ShotId Id;
        public float2 ServerPosition;
        public Angle ServerRotation;
        public int ServerTick;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProjectileLaunchSettings : INetworkSerializeByMemcpy
    {
        public float Speed;
        public float LifeTime;
        public ushort MaxRicochetCount;
    }

    public interface IProjectileBehaviour : INetworkBehaviourExtended, IProjectile
    {
        IDamage Damage { get; }
        IWeaponHolderBehaviour Owner { get; }

        void OnRicochet(float2 hitPoint, float2 surfaceNormal);
        void Initialize(Required<IProjectileConfig> config, Required<IDamage> damage);
    }

    public class ProjectileBehaviour :
        NetworkBehaviourExtended, IProjectileBehaviour, IMessageSender
    {
        public IProjectileConfig Config { get; private set; }
        public IDamage Damage { get; private set; }
        public IWeaponHolderBehaviour Owner { get; private set; }

        private readonly ProjectileMover _mover = new ProjectileMover();

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

        public void Launch(IWeaponHolderBehaviour owner)
        {
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
            ForceInitialize();
        }
    }
}
