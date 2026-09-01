using Tanks.Shared;
using Tanks.Shared.Stats;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Server.Entities;

namespace Tanks.Server.Weapons
{
    public sealed class ProjectileMovement :
        NetworkComponent<IProjectileBehaviour>,
        INetworkMessageHandler<ProjectileLaunched>
    {
        public ProjectileMovement(IProjectilePoolService pool)
        {
            _pool = pool;
        }

        private readonly IProjectilePoolService _pool;

        private IDamage _damage;
        private int _ricochetedCount;
        private float _elapsed;

        public void Handle(ProjectileLaunched message)
        {
            _damage = message.Damage;
            _ricochetedCount = 0;
            _elapsed = 0;
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (Transform.IsValid == false) return;

            if (_elapsed >= Behaviour.Config.LifeTime)
            {
                _pool.Return(Behaviour);
                return;
            }

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = (_, _) => _ricochetedCount++,
                OnDamageableHit = x => DealDamage(Behaviour, x)
            }, Transform, tickDelta);

            if (_ricochetedCount > Behaviour.Config.MaxRicochetCount)
            {
                _pool.Return(Behaviour);
                return;
            }

            _elapsed += tickDelta;

            _pool.RequestReconciliation(Behaviour, new ProjectileSnapshotFromServer
            {
                ServerPosition = Transform.WorldPosition,
                ServerRotation = Transform.WorldRotation,
                ServerTick = tick,
                ServerElapsed = _elapsed,
                ServerRicochetedCount = _ricochetedCount
            });
        }

        private void DealDamage(IProjectileBehaviour projectile, IHealthBehaviour health)
        {
            var statOwner = Behaviour.Owner.GetBehaviour<IStatOwnerBehaviour>();
            var authoritativeHealth = health.Require<IAuthoritativeHealth>();

            float amount = statOwner.GetStat(StatType.Damage, _damage.Evaluate(_elapsed));
            HealthEventId eventId = HealthEventId.Create(Behaviour, Behaviour.Id);

            authoritativeHealth.Damage(amount, Behaviour.Owner.Entity, eventId);
            _pool.Return(projectile);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
