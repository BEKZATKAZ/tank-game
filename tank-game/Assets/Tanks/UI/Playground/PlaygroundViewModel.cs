using System;
using Tanks.Audio;
using Tanks.Client.Playground;
using Tanks.Localization;
using Tanks.Shared.Messaging;
using Tanks.Utils;

namespace Tanks.UI.Playground
{
    public sealed class PlaygroundViewModel :
        IViewModel,
        IMessageHandler<PlaygroundPromptRequested>,
        IDisposable
    {
        public PlaygroundViewModel(
            ILocalizationDatabase localization, IAudioPlayer audioPlayer, IMessageBus bus)
        {
            _localization = localization;
            _audioPlayer = audioPlayer;
            _bus = bus;
            _bus.RegisterHandler<PlaygroundPromptRequested>(this);
        }

        public BindableProperty<string> Prompt { get; } = new();

        private readonly ILocalizationDatabase _localization;
        private readonly IMessageBus _bus;
        private readonly IAudioPlayer _audioPlayer;

        public void Dispose()
        {
            _bus.UnregisterHandler<PlaygroundPromptRequested>(this);
        }

        public void Handle(PlaygroundPromptRequested message, IMessageSender sender)
        {
            _localization.TryGet("Playground", $"tutorial.prompt.{message.Key}", out var prompt);

            string value = prompt?.Get();

            if (Prompt.Value == value) return;

            if (string.IsNullOrWhiteSpace(Prompt.Value) == false)
            {
                _audioPlayer.PlayOneShot("event:/UI/PlaygroundPrompt");
            }

            Prompt.Value = value;
        }
    }
}
