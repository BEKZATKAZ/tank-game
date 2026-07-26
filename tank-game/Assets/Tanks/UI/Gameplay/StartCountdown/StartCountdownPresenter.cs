using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using System;
using Tanks.Shared;
using Tanks.Shared.Messaging;

namespace Tanks.UI.Gameplay.StartCountdown
{
    public sealed class StartCountdownPresenter :
        PresenterBase<GameplayDocument, StartCountdownView, StartCountdownModel>,
        IMessageHandler<GameStarted>,
        IDisposable
    {
        public StartCountdownPresenter(IMessageBus bus)
        {
            _bus = bus;
            _bus.RegisterHandler<GameStarted>(this);
        }

        public readonly string[] CountdownText = { "3", "2", "1", "GO!" };

        private readonly IMessageBus _bus;
        private EventInstance _counter;

        public async void Handle(GameStarted message, IMessageSender sender)
        {
            if (_counter.isValid() == false && RuntimeManager.IsInitialized)
            {
                _counter = RuntimeManager.CreateInstance("event:/SFX/StartCounter");
            }

            TimeSpan delayThird = TimeSpan.FromSeconds(message.StartDelaySeconds / 3);

            for (int i = 0; i < 4; i++)
            {
                View.Show(new StartCountdownModel { Text = CountdownText[i] });
                _counter.start();
                await UniTask.Delay(delayThird);
            }

            View.Hide();
        }

        public void Dispose()
        {
            if (_counter.isValid() == false) return;
            _counter.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _counter.release();
        }
    }
}
