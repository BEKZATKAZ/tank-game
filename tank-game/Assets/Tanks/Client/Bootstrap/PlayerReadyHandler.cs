using Cysharp.Threading.Tasks;
using Tanks.Localization;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Unity.Mathematics;

namespace Tanks.Client.Bootstrap
{
    public struct GameStartedFromClient
    {
        public float AdaptedStartDelaySeconds;
    }

    public sealed class PlayerReadyHandler :
        NetworkComponent<GameStartBehaviour>,
        INetworkMessageHandler<PlayerReadyRequested>,
        INetworkMessageHandler<GameStartedFromServer>,
        IMessageSender
    {
        public PlayerReadyHandler(ILocalizationDatabase localization, IMessageBus bus)
        {
            _localization = localization;
            _bus = bus;
        }

        private readonly ILocalizationDatabase _localization;
        private readonly IMessageBus _bus;

        private bool _isInitialized;
        private int _lastTick;

        public void Handle(GameStartedFromServer message)
        {
            int tickDifference = math.max(_lastTick - message.ServerTick, 0);
            float timeDifference = tickDifference * message.ServerTickDelta;

            // Calculated to correctly adapt time-based events
            // Min 0, otherwise UniTask.Delay() will throw with negative values
            float delay = math.max(message.StartDelaySeconds - timeDifference, 0);
            var clientMessage = new GameStartedFromClient { AdaptedStartDelaySeconds = delay };

            Behaviour.EmitMessage(clientMessage);
            _bus.Publish(clientMessage, this);
        }

        public async void Handle(PlayerReadyRequested message)
        {
            if (_isInitialized == false)
            {
                await _localization.WaitForInitializationAsync();
                _isInitialized = true;
            }

            while (Behaviour.NetworkManager.LocalClient.PlayerObject == false)
            {
                await UniTask.Delay(500);
            }

            Behaviour.SendReadyConfirmationServerRpc(message.SecretHandle);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler<PlayerReadyRequested>(this);
            Behaviour.RegisterMessageHandler<GameStartedFromServer>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;
        }
    }
}
