using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;

namespace Tanks.Server.Teams
{
    public sealed class RoundAdder :
        NetworkComponent<ITeamBehaviourService>,
        IMessageHandler<GameStartedFromServer>
    {
        public RoundAdder(IMessageBus bus)
        {
            _bus = bus;
        }

        private readonly IMessageBus _bus;
        private int _currentRound;

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<GameStartedFromServer>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<GameStartedFromServer>(this);
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            Behaviour.SetRoundEveryoneRpc(++_currentRound);
        }
    }
}
