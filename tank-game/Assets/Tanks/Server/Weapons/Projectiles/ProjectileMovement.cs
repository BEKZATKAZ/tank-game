using Tanks.Shared;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Weapons.Projectiles
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
        private int _ricochetedCount;
        private bool _isActive;

        public void Handle(ProjectileLaunched message)
        {
            _isActive = true;
            _ricochetedCount = 0;
            Behaviour.ElapsedSeconds = 0;
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_isActive == false || Transform.IsValid == false) return;

            if (Behaviour.ElapsedSeconds >= Behaviour.LifeTime)
            {
                Done();
                return;
            }

            Behaviour.Simulate(new LaunchCallbacks
            {
                OnRicochet = (_, _) => _ricochetedCount++,
                OnDamageableHit = x => { Behaviour.OnDamageableHit(x); Done(); }
            }, Transform, tickDelta);

            if (_ricochetedCount > Behaviour.MaxRicochetCount)
            {
                Done();
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

        private void Done()
        {
            _isActive = false;
            _pool.Return(Behaviour.Id);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
