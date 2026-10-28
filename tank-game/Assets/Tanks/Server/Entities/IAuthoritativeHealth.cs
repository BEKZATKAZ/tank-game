using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Teams;

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
        NetworkComponent<IHealthBehaviour>, IAuthoritativeHealth,
        INetworkMessageHandler<HealthDespawned>
    {
        private const double AssistWindow = 10;

        private readonly List<(IEntityBehaviour From, float Time)> _damageHistory = new();
        private HealthState _state;
        private float _now;

        public HealthState GetState() => _state;

        public void Handle(HealthDespawned message)
        {
            _damageHistory.Clear();
        }

        public void SetState(in HealthState newState)
        {
            if (_state.Current <= 0 || newState.Current <= 0) _damageHistory.Clear();
            _state = newState;
            Behaviour.UpdateStateEveryoneRpc(_state);
        }

        public void Damage(
            uint amount,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            if (_state.Current <= 0) return;

            _damageHistory.RemoveAll(x => _now - x.Time > AssistWindow || !IsPlayer(x.From));

            if (amount > 0 && IsPlayer(from) && from != Behaviour.Entity)
            {
                _damageHistory.RemoveAll(x => x.From == from);
                _damageHistory.Add((from, _now));
            }

            _state = new HealthState((int)(_state.Current - amount), _state.Max);
            IEntityBehaviour assistant = null;

            if (_state.Current <= 0)
            {
                if (IsPlayer(Behaviour.Entity) && IsPlayer(from) && from != Behaviour.Entity)
                {
                    for (int i = _damageHistory.Count - 1; i >= 0; i--)
                    {
                        IEntityBehaviour candidate = _damageHistory[i].From;
                        if (candidate == from || candidate == Behaviour.Entity) continue;

                        assistant = candidate;
                        break;
                    }
                }

                _damageHistory.Clear();
            }

            Behaviour.UpdateStateEveryoneRpc(
                _state, from?.Id ?? default, eventId, assistant?.Id ?? default);
        }

        public void Heal(uint amount, IEntityBehaviour from = null)
        {
            _state = new HealthState((int)(_state.Current + amount), _state.Max);
            Behaviour.UpdateStateEveryoneRpc(_state, from?.Id ?? default);
        }

        protected override void OnInitialize()
        {
            _damageHistory.Clear();
            Behaviour.RegisterMessageHandler(this);

            uint initial = Behaviour.InitialHealth;
            _state = new HealthState((int)initial, initial);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _now = tick * tickDelta;
        }

        protected override void OnDisable()
        {
            _damageHistory.Clear();
        }

        protected override void OnDestroy()
        {
            _damageHistory.Clear();
        }

        private static bool IsPlayer(IEntityBehaviour entity)
        {
            return entity is { IsValid: true } &&
                entity.GetBehaviour<ITeamParticipantBehaviour>()?.State != null;
        }
    }
}
