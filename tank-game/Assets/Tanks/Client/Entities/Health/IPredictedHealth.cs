using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using VContainer;

namespace Tanks.Client.Entities.Health
{
    public struct HealthChangedFromPrediction
    {
        public HealthState OldState;
        public HealthState NewState;
        public IEntityBehaviour Target;
        public IEntityBehaviour From;
    }

    public interface IPredictedHealth : INetworkComponent, IMessageSender
    {
        HealthState GetState();
        void Damage(PositiveFloat amount, IEntityBehaviour from = null);
        void Heal(PositiveFloat amount, IEntityBehaviour from = null);
    }

    public sealed class PredictedHealth :
        NetworkComponent<IHealthBehaviour>,
        IPredictedHealth,
        INetworkMessageHandler<HealthChangedFromServer>
    {
        [Inject] private readonly IMessageBus _bus;

        private HealthState _state = new HealthState(100, 100);

        public HealthState GetState()
        {
            return _state;
        }

        public void SetState(in HealthState newState)
        {
            _state = newState;
        }

        public void Handle(HealthChangedFromServer message)
        {
            _state = message.AuthoritativeState;
            _bus.Publish(message, this);
        }

        public void Damage(PositiveFloat amount, IEntityBehaviour from = null)
        {
            HealthState oldState = _state;
            _state = new HealthState(_state.Current - amount, _state.Max);
            EmitChangedMessages(oldState, from);
        }

        public void Heal(PositiveFloat amount, IEntityBehaviour from = null)
        {
            HealthState oldState = _state;
            _state = new HealthState(_state.Current + amount, _state.Max);
            EmitChangedMessages(oldState, from);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }

        private void EmitChangedMessages(in HealthState oldState, in IEntityBehaviour from)
        {
            HealthChangedFromPrediction message = new HealthChangedFromPrediction
            {
                From = from,
                OldState = oldState,
                NewState = _state,
                Target = Behaviour.Entity
            };

            Behaviour.EmitMessage(message);
            _bus.Publish(message, this);
        }
    }
}
