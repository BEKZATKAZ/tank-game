using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;

namespace Tanks.Server.Entities
{
    public sealed class AuthoritativeDamageService : DamageServiceBase
    {
        protected override void OnDamage(
            uint fullAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default)
        {
            IAuthoritativeHealth targetHealth = target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            targetHealth.Damage(fullAmount, from, eventId);
        }

        protected override void OnHeal(uint amount, IEntityBehaviour target)
        {
            IAuthoritativeHealth targetHealth = target
                .GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>();

            targetHealth.Heal(amount, null);
        }
    }
}
