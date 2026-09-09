using Cysharp.Threading.Tasks;
using System;
using Tanks.Client.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.StartCountdown
{
    public sealed class StartCountdownViewModel :
        IViewModel, IMessageHandler<GameStartedFromClient>, IDisposable
    {
        public StartCountdownViewModel(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<GameStartedFromClient>(this);
        }

        public readonly string[] CountdownText = { "3", "2", "1", "GO!" };
        public BindableProperty<string> Title { get; } = new();

        private readonly IMessageBus _bus;

        public void Dispose()
        {
            _bus.UnregisterHandler<GameStartedFromClient>(this);
        }

        public async void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            TimeSpan delayThird = TimeSpan.FromSeconds(message.AdaptedStartDelaySeconds / 3);

            for (int i = 0; i < 4; i++)
            {
                Title.Value = CountdownText[i];
                await UniTask.Delay(delayThird);
            }

            Title.Value = "";
        }
    }
}
