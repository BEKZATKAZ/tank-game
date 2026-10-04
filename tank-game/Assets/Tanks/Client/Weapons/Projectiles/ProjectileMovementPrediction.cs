using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Lag;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Weapons.Projectiles
{
    public sealed class ProjectileMovementPrediction :
        NetworkComponent<IProjectileBehaviour>,
        INetworkMessageHandler<ProjectileSnapshotFromServer>,
        INetworkMessageHandler<ProjectileLaunched>
    {
        public ProjectileMovementPrediction(
            IProjectilePoolService pool,
            ILagCompensationGateway lagCompensation)
        {
            _pool = pool;
            _lagCompensation = lagCompensation;
        }

        public ITransform SimulatedTransform { get; private set; }

        private readonly IProjectilePoolService _pool;
        private readonly ILagCompensationGateway _lagCompensation;

        private ProjectileReconciliation _reconciliation;
        private int _ricochetedCount;
        private bool _justRicocheted;

        private int _currentTick;
        private float _currentTickDelta;

        public void Handle(ProjectileLaunched message)
        {
            SimulatedTransform.WorldPosition = Transform.WorldPosition;
            SimulatedTransform.WorldRotation = Transform.WorldRotation;
            Behaviour.ElapsedSeconds = 0;

            _reconciliation.Reset(_currentTick, SimulatedTransform);
            _ricochetedCount = 0;
        }

        public void Handle(ProjectileSnapshotFromServer message)
        {
            LaunchCallbacks callbacks = new LaunchCallbacks { OnRicochet = OnRicochetPartial };

            Behaviour.ElapsedSeconds = message.ServerElapsed;
            _ricochetedCount = message.ServerRicochetedCount;

            _reconciliation.TryReconcile(
                SimulatedTransform, message, callbacks, _currentTickDelta,
                out float2 positionError);

            if (math.length(positionError) < 3) return;

            // Snap if too distant
            Transform.WorldPosition = SimulatedTransform.WorldPosition;
        }

        protected override void OnDestroy()
        {
            _reconciliation.Dispose();
        }

        protected override void OnInitialize()
        {
            SimulatedTransform = new ProjectileTransform(Transform.WorldScale);
            _reconciliation = new ProjectileReconciliation(Behaviour);

            Behaviour.RegisterMessageHandler<ProjectileLaunched>(this);
            Behaviour.RegisterMessageHandler<ProjectileSnapshotFromServer>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            (_currentTick, _currentTickDelta) = (tick, tickDelta);
            _reconciliation.Tick(tick, SimulatedTransform);

            Behaviour.ElapsedSeconds += tickDelta;

            if (Behaviour.ElapsedSeconds >= Behaviour.LifeTime)
            {
                Done();
                return;
            }

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = OnRicochetFull,
                OnDamageableHit = OnDamageableHit
            }, SimulatedTransform, tickDelta);
        }

        protected override void OnUpdate(float deltaTime)
        {
            float lerpSpeed = _justRicocheted ? 80 : 20;
            float t = 1 - math.exp(-lerpSpeed * deltaTime);

            Transform.WorldPosition = math.lerp(
                Transform.WorldPosition, SimulatedTransform.WorldPosition, t);

            Transform.WorldRotation = SimulatedTransform.WorldRotation;

            _justRicocheted = false;
        }

        private void OnRicochetFull(float2 hitPoint, float2 surfaceNormal)
        {
            if (++_ricochetedCount > Behaviour.MaxRicochetCount)
            {
                Done();
                return;
            }

            Behaviour.OnRicochet(hitPoint, surfaceNormal);
            _justRicocheted = true;
        }

        private void OnRicochetPartial(float2 hitPoint, float2 surfaceNormal)
        {
            if (++_ricochetedCount <= Behaviour.MaxRicochetCount) return;
            Done();
        }

        private void OnDamageableHit(IHealthBehaviour target)
        {
            Behaviour.OnDamageableHit(target);

            _lagCompensation.RequestHitServerRpc(new HitRequest
            {
                ClientTick = _currentTick,
                ProjectileId = Behaviour.Id,
                TargetId = target.Entity.Id
            });

            Done();
        }

        private void Done()
        {
            _pool.Return(Behaviour.Id);
        }
    }
}
