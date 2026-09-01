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
        void Damage(PositiveFloat amount, IEntityBehaviour from = null);
        void Heal(PositiveFloat amount, IEntityBehaviour from = null);
    }

    public sealed class AuthoritativeHealth :
        NetworkComponent<IHealthBehaviour>, IAuthoritativeHealth
    {
        private HealthState _state = new HealthState(100, 100);
        private IEntityBehaviour _lastFrom;

        public HealthState GetState() => _state;
        public void SetState(in HealthState newState) => _state = newState;

        public void Damage(PositiveFloat amount, IEntityBehaviour from = null)
        {
            _state = new HealthState(_state.Current - amount, _state.Max);
            _lastFrom = from;
        }

        public void Heal(PositiveFloat amount, IEntityBehaviour from = null)
        {
            _state = new HealthState(_state.Current + amount, _state.Max);
            _lastFrom = from;
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            Behaviour.UpdateStateEveryoneRpc(tick, _state, _lastFrom?.Id ?? default);
        }
    }
}
