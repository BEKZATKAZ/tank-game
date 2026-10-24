using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Client.Entities.Health
{
    public sealed class DamagePredictionService : DamageServiceBase
    {
        protected override void OnDamage(
            uint fullAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            IPredictedHealth targetHealth = target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IPredictedHealth>();

            targetHealth.Damage(fullAmount, from, eventId);
        }

        protected override void OnHeal(uint amount, IEntityBehaviour target)
        {
            IPredictedHealth targetHealth = target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IPredictedHealth>();

            targetHealth.Heal(amount, null);
        }
    }
}
