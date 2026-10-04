using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using VContainer;

namespace Tanks.Client.Entities.Health
{
    public struct HealthChangedFromPrediction
    {
        public HealthEventId EventId;
        public HealthState OldState;
        public HealthState NewState;
        public IEntityBehaviour Target;
        public IEntityBehaviour From;
    }

    public interface IPredictedHealth : INetworkComponent, IMessageSender
    {
        HealthState GetState();

        void Damage(
            PositiveFloat amount,
            IEntityBehaviour from = null,
            HealthEventId damageId = default);

        void Heal(PositiveFloat amount, IEntityBehaviour from = null);
    }

    public sealed class PredictedHealth :
        NetworkComponent<IHealthBehaviour>,
        IPredictedHealth,
        INetworkMessageHandler<HealthChangedFromServer>
    {
        [Inject] private readonly IMessageBus _bus;
        
        private readonly RingBuffer<HealthEventId> _eventBuffer = new(16);
        private HealthState _state = new HealthState(100, 100);
        private HealthEventId _enqueued;
        private int _count;

        public HealthState GetState() => _state;
        public void SetState(in HealthState newState) => _state = newState;

        public void Handle(HealthChangedFromServer message)
        {
            _state = message.AuthoritativeState;
            HealthEventId eventId = message.EventId;

            if (eventId.Equals(default) == false && TryEnqueueHealthEvent(eventId))
            {
                EmitChangedMessages(message.OldState, default, eventId);
            }
        }

        public void Damage(
            PositiveFloat amount,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            if (TryEnqueueHealthEvent(eventId) == false)  return;

            HealthState oldState = _state;
            _state = new HealthState(_state.Current - amount, _state.Max);

            EmitChangedMessages(oldState, from, eventId);
        }

        public void Heal(PositiveFloat amount, IEntityBehaviour from = null)
        {
            HealthState oldState = _state;
            _state = new HealthState(_state.Current + amount, _state.Max);
            EmitChangedMessages(oldState, from, default);
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }

        protected override void OnDestroy()
        {
            _eventBuffer.Dispose();
        }

        protected override void OnLateUpdate(float deltaTime)
        {
            if (_enqueued.Equals(default)) return;
            _eventBuffer[_count++] = _enqueued;
            _enqueued = default;
        }

        private bool TryEnqueueHealthEvent(HealthEventId id)
        {
            if (id.Equals(default)) return true;

            for (int i = 15; i >= 0; i--)
            {
                if (_eventBuffer[i].Equals(id)) return false;
            }

            _enqueued = id;
            return true;
        }

        private void EmitChangedMessages(
            in HealthState oldState, in IEntityBehaviour from, in HealthEventId eventId)
        {
            HealthChangedFromPrediction message = new HealthChangedFromPrediction
            {
                EventId = eventId,
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
