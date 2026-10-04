using Tanks.Core.Helpers;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Entities
{
    public sealed class AuthoritativeDamageService : DamageServiceBase
    {
        protected override void OnDamage(
            PositiveFloat fullAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            IAuthoritativeHealth targetHealth = target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            targetHealth.Damage(fullAmount, from, eventId);
        }

        protected override void OnHeal(PositiveFloat amount, IEntityBehaviour target)
        {
            IAuthoritativeHealth targetHealth = target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            targetHealth.Heal(amount, null);
        }
    }
}
