using Cysharp.Threading.Tasks;
using System;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.Utils;

namespace Tanks.UI.Gameplay.StartCountdown
{
    public sealed class StartCountdownViewModel :
        IViewModel, IMessageHandler<GameStarted>, IDisposable
    {
        public StartCountdownViewModel(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<GameStarted>(this);
        }

        public readonly string[] CountdownText = { "3", "2", "1", "GO!" };
        public BindableProperty<string> Title { get; } = new();

        private readonly IMessageBus _bus;

        public void Dispose()
        {
            _bus.UnregisterHandler<GameStarted>(this);
        }

        public async void Handle(GameStarted message, IMessageSender sender)
        {
            TimeSpan delayThird = TimeSpan.FromSeconds(message.StartDelaySeconds / 3);

            for (int i = 0; i < 4; i++)
            {
                Title.Value = CountdownText[i];
                await UniTask.Delay(delayThird);
            }

            Title.Value = "";
        }
    }
}
