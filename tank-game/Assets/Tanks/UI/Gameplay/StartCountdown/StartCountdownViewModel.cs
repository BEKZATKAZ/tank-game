using Cysharp.Threading.Tasks;
using System;
using Tanks.Audio;
using Tanks.Client.Bootstrap;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using Unity.Mathematics;

namespace Tanks.UI.Gameplay.StartCountdown
{
    public sealed class StartCountdownViewModel :
        IViewModel,
        IMessageHandler<GameStartedFromClient>,
        IMessageHandler<TilemapGenerated>,
        IDisposable
    {
        public StartCountdownViewModel(IMessageBus bus, IAudioPlayer audioPlayer)
        {
            _bus = bus;
            _audioPlayer = audioPlayer;

            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<GameStartedFromClient>(this);
        }

        public readonly string[] CountdownText = { "3", "2", "1", "GO!" };
        public BindableProperty<string> Title { get; } = new();
        public BindableProperty<float> TransitionOpacity { get; } = new();

        private readonly IMessageBus _bus;
        private readonly IAudioPlayer _audioPlayer;

        public void Dispose()
        {
            _bus.UnregisterHandler<GameStartedFromClient>(this);
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            TransitionOpacity.Value = 1;
            _audioPlayer.PlayOneShot("event:/UI/RoundTransition");
        }

        public async void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            TransitionOpacity.Value = 0;

            if (message.AdaptedStartDelaySeconds > 1.5f)
            {
                float remainder = message.AdaptedStartDelaySeconds - 1.5f;
                await UniTask.Delay(TimeSpan.FromSeconds(remainder));
            }

            float total = math.min(1.5f, message.AdaptedStartDelaySeconds);
            TimeSpan delayThird = TimeSpan.FromSeconds(total / 3);

            for (int i = 0; i < 4; i++)
            {
                Title.Value = CountdownText[i];
                await UniTask.Delay(delayThird);
            }

            Title.Value = "";
        }
    }
}
