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

        public void Handle(ProjectileLaunched message)
        {
            _damage = message.Damage;
            _ricochetedCount = 0;
            Behaviour.ElapsedSeconds = 0;
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (Transform.IsValid == false) return;

            if (Behaviour.ElapsedSeconds >= Behaviour.Config.LifeTime)
            {
                _pool.Return(Behaviour.Id);
                return;
            }

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = (_, _) => _ricochetedCount++,
                OnDamageableHit = DealDamage
            }, Transform, tickDelta);

            if (_ricochetedCount > Behaviour.Config.MaxRicochetCount)
            {
                _pool.Return(Behaviour.Id);
                return;
            }

            Behaviour.ElapsedSeconds += tickDelta;

            _pool.RequestReconciliation(Behaviour, new ProjectileSnapshotFromServer
            {
                ServerPosition = Transform.WorldPosition,
                ServerRotation = Transform.WorldRotation,
                ServerTick = tick,
                ServerElapsed = Behaviour.ElapsedSeconds,
                ServerRicochetedCount = _ricochetedCount
            });
        }

        private void DealDamage(IHealthBehaviour health)
        {
            var statOwner = Behaviour.Owner.GetBehaviour<IStatOwnerBehaviour>();
            var authoritativeHealth = health.Require<IAuthoritativeHealth>();

            float amount = _damage.Evaluate(Behaviour.ElapsedSeconds);
            amount = statOwner.GetStat(StatType.Damage, amount);

            var eventId = HealthEventId.Create(typeof(IProjectileBehaviour), Behaviour.Id);
            authoritativeHealth.Damage(amount, Behaviour.Owner.Entity, eventId);

            _pool.ReturnProjectileClientRpc(Behaviour.Id);
            _pool.Return(Behaviour.Id);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
