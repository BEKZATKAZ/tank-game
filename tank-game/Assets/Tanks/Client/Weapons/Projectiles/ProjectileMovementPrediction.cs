using System;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Health;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Weapons
{
    public struct ProjectileSnapshot
    {
        public float2 Position;
        public Angle Rotation;
    }

    public class ProjectileTransform : ITransform
    {
        public ProjectileTransform(float2 worldScale) => WorldScale = worldScale;

        public bool IsValid => true;
        public float2 WorldPosition { get; set; }
        public Angle WorldRotation { get; set; }
        public float2 WorldScale { get; }

        // These are not used in projectile movement
        public float2 LocalPosition
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public Angle LocalRotation
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public float2 LocalScale
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }

    public sealed class ProjectileMovementPrediction :
        NetworkComponent<ProjectileBehaviour>,
        INetworkMessageHandler<ProjectileSnapshotFromServer>,
        INetworkMessageHandler<ProjectileLaunched>
    {
        [Inject] private readonly IProjectilePoolService _pool;

        private ProjectileReconciliation _reconciliation;
        private ITransform _predicted;
        private float2 _positionError;
        private float _elapsed;
        private int _ricochetedCount;

        public void Handle(ProjectileLaunched message)
        {
            _predicted.WorldPosition = Behaviour.Transform.WorldPosition;
            _predicted.WorldRotation = Behaviour.Transform.WorldRotation;

            int tick = NetworkManager.NetworkTickSystem.LocalTime.Tick;
            _reconciliation.Reset(tick, _predicted);
            _positionError = float2.zero;
            _elapsed = 0;
            _ricochetedCount = 0;
        }

        public void Handle(ProjectileSnapshotFromServer message)
        {
            float tickDelta = 1f / NetworkManager.NetworkTickSystem.TickRate;

            if (_reconciliation.TryReconcile(
                _predicted, message, tickDelta, out float2 newPositionError) == false) return;

            _positionError = newPositionError;
        }

        public override void OnTick(int tick, float tickDelta)
        {
            _reconciliation.Tick(tick, _predicted);
            _elapsed += tickDelta;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _reconciliation.Dispose();
        }

        protected override void Awake()
        {
            base.Awake();

            _predicted = new ProjectileTransform(Behaviour.Transform.WorldScale);
            _reconciliation = new ProjectileReconciliation(Behaviour);

            Behaviour.RegisterMessageHandler<ProjectileLaunched>(this);
            Behaviour.RegisterMessageHandler<ProjectileSnapshotFromServer>(this);
        }

        private void Update()
        {
            if (_elapsed >= Behaviour.Config.LifeTime)
            {
                _pool.Return(Behaviour);
                return;
            }

            float deltaTime = Time.deltaTime;

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = OnRicochet,
                OnDamageableHit = DamagePrediction
            }, _predicted, deltaTime);

            // Interpolate to the real position
            _positionError *= math.exp(-15 * deltaTime);

            Behaviour.Transform.WorldPosition = _predicted.WorldPosition + _positionError;
            Behaviour.Transform.WorldRotation = _predicted.WorldRotation;
        }

        private void OnRicochet(float2 hitPoint)
        {
            if (++_ricochetedCount > Behaviour.Config.MaxRicochetCount)
            {
                _pool.Return(Behaviour);
                return;
            }

            Behaviour.OnRicochet(hitPoint);
        }

        private void DamagePrediction(IDamageable damageable)
        {
            float amount = Behaviour.Damage.Evaluate(_elapsed);
            damageable.Damage(amount);
            _pool.Return(Behaviour);
        }
    }
}
