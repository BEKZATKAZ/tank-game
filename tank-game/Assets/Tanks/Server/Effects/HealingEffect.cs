using Tanks.Shared;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Effects
{
    public sealed class HealingEffect : NetworkComponent<IEffectOwnerBehaviour>
    {
        public HealingEffect(IDamageService damageService)
        {
            _damageService = damageService;
        }

        private readonly IDamageService _damageService;

        protected override void OnTick(int tick, float tickDelta)
        {
            if (Behaviour.Effects.ContainsKey(EffectType.Healing) == false) return;
            if (tick % 5 != 0) return;

            _damageService.Heal(5, Behaviour.Entity);
        }
    }
}
