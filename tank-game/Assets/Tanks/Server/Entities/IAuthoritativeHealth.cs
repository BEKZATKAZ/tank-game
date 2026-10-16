using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Entities
{
    public interface IAuthoritativeHealth : INetworkComponent
    {
        HealthState GetState();
        void SetState(in HealthState newState);

        void Damage(
            PositiveFloat amount, 
            IEntityBehaviour from = null, 
            HealthEventId eventId = default);

        void Heal(PositiveFloat amount, IEntityBehaviour from = null);
    }

    public sealed class AuthoritativeHealth :
        NetworkComponent<IHealthBehaviour>, IAuthoritativeHealth
    {
        private HealthState _state;

        public HealthState GetState() => _state;

        public void SetState(in HealthState newState)
        {
            _state = newState;
            Behaviour.UpdateStateEveryoneRpc(_state);
        }

        public void Damage(
            PositiveFloat amount,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            _state = new HealthState(_state.Current - amount, _state.Max);
            Behaviour.UpdateStateEveryoneRpc(_state, from?.Id ?? default, eventId);
        }

        public void Heal(PositiveFloat amount, IEntityBehaviour from = null)
        {
            _state = new HealthState(_state.Current + amount, _state.Max);
            Behaviour.UpdateStateEveryoneRpc(_state, from?.Id ?? default);
        }

        protected override void OnInitialize()
        {
            float initial = Behaviour.InitialHealth;
            _state = new HealthState(initial, initial);
        }
    }
}
