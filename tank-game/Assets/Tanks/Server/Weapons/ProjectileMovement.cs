using Tanks.Shared;
using Tanks.Shared.Stats;
using Tanks.Shared.Health;
using Tanks.Shared.Weapons.Projectiles;
using VContainer;

namespace Tanks.Server.Weapons
{
    public sealed class ProjectileMovement :
        NetworkComponent<ProjectileBehaviour>,
        INetworkMessageHandler<ProjectileLaunched>
    {
        [Inject] private readonly IProjectilePoolService _pool;

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
            if (Behaviour.Transform.IsValid == false) return;

            if (_elapsed >= Behaviour.Config.LifeTime)
            {
                _pool.Return(Behaviour);
                return;
            }

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = _ => _ricochetedCount++,
                OnDamageableHit = x => DealDamage(Behaviour, x)
            }, Behaviour.Transform, tickDelta);

            if (_ricochetedCount > Behaviour.Config.MaxRicochetCount)
            {
                _pool.Return(Behaviour);
                return;
            }

            _pool.RequestReconciliation(Behaviour, new ProjectileSnapshotFromServer
            {
                ServerPosition = Behaviour.Transform.WorldPosition,
                ServerRotation = Behaviour.Transform.WorldRotation,
                ServerTick = tick
            });

            _elapsed += tickDelta;
        }

        private void DealDamage(IProjectile projectile, IDamageable damageable)
        {
            float amount = _damage.Evaluate(_elapsed);
            amount = Behaviour.Owner.EffectOwner.GetStat(StatType.Damage, amount);

            damageable.Damage(amount);
            _pool.Return(projectile);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
