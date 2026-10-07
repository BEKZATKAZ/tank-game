using Tanks.Client.Visuals;
using Tanks.Shared;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Entities.Health
{
    public sealed class DamagePreviewEffect :
        NetworkComponent<IHealthBehaviour>,
        INetworkMessageHandler<HealthChangedFromPrediction>
    {
        public DamagePreviewEffect(IVisualService service)
        {
            _service = service;
        }

        private readonly IVisualService _service;

        public void Handle(HealthChangedFromPrediction message)
        {
            if (message.OldState.Current <= message.NewState.Current) return;
            if (Behaviour.Entity.IsPossessed) return;

            float current = message.NewState.Current;
            float old = message.OldState.Current;

            DamagePreviewText text = _service.Play<DamagePreviewText>(VisualType.DamageText);
            text.Initialize(old - current, message.NewState, Transform);
            text.Animate();
        }

        protected override void OnInitialize()
        {
            Behaviour.RegisterMessageHandler(this);
        }
    }
}
