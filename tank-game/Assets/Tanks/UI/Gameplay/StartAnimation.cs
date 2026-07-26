using Cysharp.Threading.Tasks;
using FMOD.Studio;
using FMODUnity;
using System;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay
{
    public sealed class StartAnimation :
        IDocumentController<GameplayDocument>,
        IDocumentInitialize,
        IDocumentDestroy,
        IMessageHandler<GameStarted>
    {
        public StartAnimation(IMessageBus bus)
        {
            _bus = bus;
            _counter = RuntimeManager.CreateInstance("event:/SFX/StartCounter");
        }

        private readonly IMessageBus _bus;
        private readonly EventInstance _counter;

        private static readonly string[] _text = { "3", "2", "1", "GO!" };
        private Label _label;

        public async void Handle(GameStarted message, IMessageSender sender)
        {
            TimeSpan delayThird = TimeSpan.FromSeconds(message.StartDelaySeconds / 3);
            _label.style.display = DisplayStyle.Flex;

            for (int i = 0; i < 4; i++)
            {
                _label.text = _text[i];
                _counter.start();
                await UniTask.Delay(delayThird);
            }

            _label.text = "";
            _label.style.display = DisplayStyle.None;
        }

        public void Initialize(VisualElement root)
        {
            _label = root.Q<Label>("StartText");
            _label.style.display = DisplayStyle.None;
            _bus.RegisterHandler<GameStarted>(this);
        }

        public void OnDestroy()
        {
            _counter.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _counter.release();
        }
    }
}
