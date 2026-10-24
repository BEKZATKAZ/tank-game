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
            uint amount, 
            IEntityBehaviour from = null, 
            HealthEventId eventId = default);

        void Heal(uint amount, IEntityBehaviour from = null);
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
            uint amount,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            _state = new HealthState((int)(_state.Current - amount), _state.Max);
            Behaviour.UpdateStateEveryoneRpc(_state, from?.Id ?? default, eventId);
        }

        public void Heal(uint amount, IEntityBehaviour from = null)
        {
            _state = new HealthState((int)(_state.Current + amount), _state.Max);
            Behaviour.UpdateStateEveryoneRpc(_state, from?.Id ?? default);
        }

        protected override void OnInitialize()
        {
            uint initial = Behaviour.InitialHealth;
            _state = new HealthState((int)initial, initial);
        }
    }
}
