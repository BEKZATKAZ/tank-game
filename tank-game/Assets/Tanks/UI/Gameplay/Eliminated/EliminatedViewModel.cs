using Tanks.Localization;
using Tanks.Shared.Health;
using Tanks.Shared.Messaging;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.Eliminated
{
    public sealed class EliminatedViewModel : IViewModel, IMessageHandler<DiedLocal>
    {
        public EliminatedViewModel(IMessageBus bus, ILocalizationDatabase localization)
        {
            _bus = bus;
            _bus.RegisterHandler<DiedLocal>(this);
        }

        public BindableProperty<string> Title { get; } = new();
        public BindableProperty<string> Subtitle { get; } = new();

        private readonly IMessageBus _bus;
        private readonly ILocalizationDatabase _localization;

        public void Handle(DiedLocal message, IMessageSender sender)
        {
            Title.Value = "ELIMINATED";
            Subtitle.Value = "";
        }
    }
}