using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;

namespace Tanks.Server.Teams
{
    public sealed class PlayerPointAdder :
        NetworkComponent<ITeamBehaviourService>,
        IMessageHandler<Died>
    {
        public PlayerPointAdder(IMessageBus bus)
        {
            _bus = bus;
        }

        private readonly IMessageBus _bus;

        protected override void OnInitialize()
        {
            _bus.RegisterHandler<Died>(this);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<Died>(this);
        }

        public void Handle(Died message, IMessageSender sender = null)
        {
            // Discard non-player entities
            if (message.Target.GetBehaviour<ITeamParticipantBehaviour>() is null) return;

            PlayerState from;

            if (AreTeammates(message.Target, message.From))
            {
                // Penalty
                TryAddPointsTo(message.From, PlayerPointType.Eliminations, -1, out from);
            }
            else
            {
                // Normal elimination
                TryAddPointsTo(message.From, PlayerPointType.Eliminations, 1, out from);
            }

            TryAddPointsTo(message.Target, PlayerPointType.Deaths, 1, out PlayerState target);

            if (target != null) Behaviour.SyncPlayerState(target);
            if (from != null) Behaviour.SyncPlayerState(from);

            if (from is null || target is null || from.PlayerId == target.PlayerId) return;

            ITeamParticipantBehaviour assistant = message.Assistant?
                .GetBehaviour<ITeamParticipantBehaviour>();

            if (assistant?.State is null) return;
            if (assistant.State.PlayerId == from.PlayerId ||
                assistant.State.PlayerId == target.PlayerId) return;

            if (TryAddPointsTo(message.Assistant, PlayerPointType.Assistance, 1, out var helper))
            {
                Behaviour.SyncPlayerState(helper);
            }
        }

        private bool AreTeammates(IEntityBehaviour a, IEntityBehaviour b)
        {
            ITeamParticipantBehaviour participantA = a?
                .GetBehaviour<ITeamParticipantBehaviour>();

            ITeamParticipantBehaviour participantB = b?
                .GetBehaviour<ITeamParticipantBehaviour>();

            if (participantA is null || participantB is null) return false;

            return participantA.BelongsTo == participantB.BelongsTo;
        }

        private bool TryAddPointsTo(
            IEntityBehaviour target, PlayerPointType type, int amount, out PlayerState state)
        {
            state = null;

            if (target is not { IsValid: true }) return false;

            ITeamParticipantBehaviour participant = target
                .GetBehaviour<ITeamParticipantBehaviour>();

            if (participant is not { State: not null }) return false;

            state = participant.State;
            state.AddPoints(type, amount);

            return true;
        }
    }
}
