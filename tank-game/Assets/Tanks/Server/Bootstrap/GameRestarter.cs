using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Server.Bootstrap
{
    public sealed class GameRestarter :
        NetworkComponent<IGameStartBehaviour>,
        IMessageHandler<Died>
    {
        public GameRestarter(IMessageBus bus, IProjectilePoolService pool)
        {
            _bus = bus;
            _pool = pool;
            _bus.RegisterHandler<Died>(this);
        }

        private readonly IMessageBus _bus;
        private readonly IProjectilePoolService _pool;

        private int _currentTick;
        private int _lastDiedTick = -1;

        public void Handle(Died message, IMessageSender sender)
        {
            _lastDiedTick = _currentTick;
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<Died>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _currentTick = tick;

            if (_lastDiedTick == -1) return;

            float delaySeconds = 2.5f;
            int delayTicks = (int)math.ceil(delaySeconds / tickDelta);

            if (tick - _lastDiedTick < delayTicks) return;
            _lastDiedTick = -1;

            // Restart when only one left
            if (Behaviour.GetAlivePlayerCount() > 1) return;

            _pool.ReturnAll();
            Behaviour.PrepareGame();
        }
    }
}
