using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Collections;
using Unity.Mathematics;

namespace Tanks.Server.Bootstrap
{
    public sealed class GameRestarter :
        NetworkComponent<IGameStartBehaviour>,
        IMessageHandler<Died>,
        INetworkMessageHandler<GameStarted>
    {
        public GameRestarter(
            IMessageBus bus,
            IProjectilePoolService pool,
            IConnectedClientService clientService)
        {
            _bus = bus;
            _pool = pool;
            _clientService = clientService;
            _bus.RegisterHandler<Died>(this);
        }

        private readonly IMessageBus _bus;
        private readonly IProjectilePoolService _pool;
        private readonly IConnectedClientService _clientService;

        private int _currentTick;
        private int _lastDiedTick = -1;
        private NativeHashSet<ulong> _eliminatedPlayerIds;

        public void Handle(Died message, IMessageSender sender)
        {
            if (message.Target.OwnerClientId == 0) return;
            _eliminatedPlayerIds.Add(message.Target.OwnerClientId);
            _lastDiedTick = _currentTick;
        }

        public void Handle(GameStarted message)
        {
            _eliminatedPlayerIds.Clear();
            _lastDiedTick = -1;
        }

        protected override void OnInitialize()
        {
            _eliminatedPlayerIds = new NativeHashSet<ulong>(4, Allocator.Persistent);
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnDestroy()
        {
            _eliminatedPlayerIds.Dispose();
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

            int connectedClientCount = _clientService.ConnectedClientCount;
            int aliveCount = connectedClientCount - _eliminatedPlayerIds.Count;

            // Restart when only one left
            if (aliveCount > 1) return;

            _pool.ReturnAll();
            Behaviour.PrepareGame();
        }
    }
}
