using Tanks.Core.Helpers;
using Tanks.Shared.Stats;

namespace Tanks.Shared.Entities.Health
{
    public interface IDamageService
    {
        void Damage(
            PositiveFloat baseAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default,
            bool includeStats = true);

        void Heal(PositiveFloat amount, IEntityBehaviour target);
    }

    public abstract class DamageServiceBase : IDamageService
    {
        protected abstract void OnDamage(
            PositiveFloat fullAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default);

        protected abstract void OnHeal(
            PositiveFloat amount, IEntityBehaviour target);

        public void Damage(
            PositiveFloat baseAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default,
            bool includeStats = true)
        {
            if (includeStats == false)
            {
                OnDamage(baseAmount, target, from, eventId);
                return;
            }

            IStatOwnerBehaviour targetStat = target.GetBehaviour<IStatOwnerBehaviour>();
            IStatOwnerBehaviour fromStat = from?.GetBehaviour<IStatOwnerBehaviour>();

            float damage = fromStat?.GetStat(StatType.Damage, baseAmount) ?? baseAmount;
            float defense = targetStat.GetStat(StatType.Defense, 0);
            float multiplier = targetStat.GetDiminishingReturns(StatType.Defense, defense);

            OnDamage(damage * multiplier, target, from, eventId);
        }

        public void Heal(PositiveFloat amount, IEntityBehaviour target)
        {
            OnHeal(amount, target);
        }
    }
}
