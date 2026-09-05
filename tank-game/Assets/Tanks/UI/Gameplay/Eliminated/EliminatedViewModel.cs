using System;
using Tanks.Client.Entities;
using Tanks.Localization;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Messaging;
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

            if (message.From == message.Target)
            {
                _localization.TryGet("Gameplay", "gameplay.eliminated-by-self", out var self);
                EliminatedBy.Value = self?.Get();
                return;
            }

            if (message.From is null || string.IsNullOrWhiteSpace(message.From.DisplayName))
            {
                EliminatedBy.Value = "";
                return;
            }

            _localization.TryGet("Gameplay", "gameplay.eliminated-by", out var by);
            by?.AddVariable("name", message.From.DisplayName);
            EliminatedBy.Value = by?.Get();
        }
    }
}
