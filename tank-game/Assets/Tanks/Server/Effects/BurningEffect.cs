using Tanks.Shared;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Effects
{
    public sealed class BurningEffect : NetworkComponent<IEffectOwnerBehaviour>
    {
        public BurningEffect(IDamageService damageService)
        {
            _damageService = damageService;
        }

        public const int Damage = 6;

        private readonly IDamageService _damageService;

        protected override void OnTick(int tick, float tickDelta)
        {
            if (Behaviour.Effects.TryGetValue(
                EffectType.Burning, out EffectEntry effect) == false) return;

            if (tick % 10 != 0) return;

            HealthEventId eventId = HealthEventId.Create(Behaviour.GetType(), tick);
            _damageService.Damage(Damage, Behaviour.Entity, effect.From, eventId, includeStats: false);
        }
    }
}
