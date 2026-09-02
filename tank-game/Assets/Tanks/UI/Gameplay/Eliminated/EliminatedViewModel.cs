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
        public EliminatedViewModel(
            IMessageBus bus,
            IClientEntityService clientEntityService,
            ILocalizationDatabase localization)
        {
            _clientEntityService = clientEntityService;
            _localization = localization;

            _bus = bus;
            _bus.RegisterHandler<Died>(this);
        }

        public BindableProperty<string> Title { get; } = new();
        public BindableProperty<string> Subtitle { get; } = new();

        private readonly IMessageBus _bus;
        private readonly IClientEntityService _clientEntityService;
        private readonly ILocalizationDatabase _localization;

        public void Dispose()
        {
            _bus.UnregisterHandler<Died>(this);
        }

        public void Handle(Died message, IMessageSender sender)
        {
            if (_clientEntityService.GetPossessedEntity() != message.Target) return;
            Title.Value = "ELIMINATED";
            Subtitle.Value = "";
        }
    }
}
