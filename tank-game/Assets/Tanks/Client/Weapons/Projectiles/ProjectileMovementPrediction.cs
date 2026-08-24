using System;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Stats;
using Tanks.Shared.Health;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

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
        NetworkComponent<IProjectileBehaviour>,
        INetworkMessageHandler<ProjectileSnapshotFromServer>,
        INetworkMessageHandler<ProjectileLaunched>
    {
        public ProjectileMovementPrediction(IProjectilePoolService pool)
        {
            _pool = pool;
        }

        private readonly IProjectilePoolService _pool;

        private ProjectileReconciliation _reconciliation;
        private ITransform _predicted;
        private float2 _positionError;
        private float _elapsed;
        private int _ricochetedCount;

        private int _currentTick;
        private float _currentTickDelta;

        public void Handle(ProjectileLaunched message)
        {
            _predicted.WorldPosition = Transform.WorldPosition;
            _predicted.WorldRotation = Transform.WorldRotation;

            _reconciliation.Reset(_currentTick, _predicted);
            _positionError = float2.zero;
            _elapsed = 0;
            _ricochetedCount = 0;
        }

        public void Handle(ProjectileSnapshotFromServer message)
        {
            if (_reconciliation.TryReconcile(
                _predicted, message, _currentTickDelta, out float2 newPositionError) == false) return;

            _positionError = newPositionError;
        }

        protected override void OnDestroy()
        {
            _reconciliation.Dispose();
        }

        protected override void OnInitialize()
        {
            _predicted = new ProjectileTransform(Transform.WorldScale);
            _reconciliation = new ProjectileReconciliation(Behaviour);

            Behaviour.RegisterMessageHandler<ProjectileLaunched>(this);
            Behaviour.RegisterMessageHandler<ProjectileSnapshotFromServer>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            (_currentTick, _currentTickDelta) = (tick, tickDelta);
            _reconciliation.Tick(tick, _predicted);
            _elapsed += tickDelta;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (_elapsed >= Behaviour.Config.LifeTime)
            {
                _pool.Return(Behaviour);
                return;
            }

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = OnRicochet,
                OnDamageableHit = DamagePrediction
            }, _predicted, deltaTime);

            // Interpolate to the real position
            _positionError *= math.exp(-15 * deltaTime);

            Transform.WorldPosition = _predicted.WorldPosition + _positionError;
            Transform.WorldRotation = _predicted.WorldRotation;
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
            amount = Behaviour.Owner.EffectOwner.GetStat(StatType.Damage, amount);

            damageable.Damage(amount);
            _pool.Return(Behaviour);
        }
    }
}
