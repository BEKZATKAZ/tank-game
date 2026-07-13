using Cysharp.Threading.Tasks;
using LitMotion;
using Tanks.Shared.Health;
using Tanks.Shared.Messaging;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI.Gameplay
{
    public sealed class EliminatedAnimation :
        IDocumentController<GameplayDocument>,
        IDocumentInitialize,
        IMessageHandler<DiedLocal>
    {
        [Inject] private readonly IMessageBus _bus;

        private VisualElement _container;
        private Label _reason;

        public async void Handle(DiedLocal message, IMessageSender sender)
        {
            _container.style.display = DisplayStyle.Flex;
            _reason.text = ""; // TODO: Add reasons

            await LMotion.Create(100f, 0, duration: 1.25f)
                .WithDelay(1.2f)
                .WithEase(Ease.OutSine)
                .Bind(x => _container.style.opacity = x);

            _container.style.display = DisplayStyle.None;
        }

        public void Initialize(VisualElement root)
        {
            _container = root.Q("EliminatedContainer");
            _reason = _container.Q<Label>("EliminatedBy");

            _container.style.display = DisplayStyle.None;
            _bus.RegisterHandler<DiedLocal>(this);
        }
    }
}