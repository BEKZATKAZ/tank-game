using Tanks.Client.Visuals;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Entities.Health
{
    public sealed class DamagePreviewEffect :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<DamageTaken>
    {
        public DamagePreviewEffect(IVisualService service)
        {
            _service = service;
        }

        private readonly IVisualService _service;

        public void Handle(DamageTaken message)
        {
            DamagePreviewText text = _service.Play<DamagePreviewText>(VisualType.DamageText, default);
            text.Initialize(message.Amount, Transform);
            text.Animate();
        }

        protected override void OnInitialize()
        {
            if (Behaviour.IsLocalPlayer)
            {
                IsEnabled = false;
                return;
            }

            Behaviour.RegisterMessageHandler(this);
        }
    }
}
