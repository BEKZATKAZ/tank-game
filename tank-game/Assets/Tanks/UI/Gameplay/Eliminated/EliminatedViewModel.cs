using System;
using Tanks.Localization;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
using Tanks.Shared.Teams;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.Eliminated
{
    public sealed class EliminatedViewModel : IViewModel, IMessageHandler<Died>, IDisposable
    {
        public EliminatedViewModel(IMessageBus bus, ILocalizationDatabase localization)
        {
            _localization = localization;
            _bus = bus;
            _bus.RegisterHandler<Died>(this);
        }

        public BindableProperty<string> EliminatedBy { get; } = new();

        private readonly IMessageBus _bus;
        private readonly ILocalizationDatabase _localization;

        public void Dispose()
        {
            _bus.UnregisterHandler<Died>(this);
        }

        public void Handle(Died message, IMessageSender sender)
        {
            if (message.Target.IsPossessed == false) return;

            // Prevent writing the same values (it will not be updated)
            EliminatedBy.Value = "";

            if (message.From == message.Target)
            {
                _localization.TryGet("Gameplay", "gameplay.eliminated-by-self", out var self);
                EliminatedBy.Value = self?.Get();
                return;
            }

            ITeamParticipantBehaviour participant = message.From?
                .GetBehaviour<ITeamParticipantBehaviour>();

            if (participant is null) return;

            _localization.TryGet("Gameplay", "gameplay.eliminated-by", out var by);
            by?.AddVariable("name", participant.State.DisplayName);

            EliminatedBy.Value = by?.Get();
        }
    }
}
