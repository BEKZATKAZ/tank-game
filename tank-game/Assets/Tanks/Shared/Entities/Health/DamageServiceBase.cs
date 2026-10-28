using System;
using System.Linq;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Stats;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Entities.Health
{
    public interface IDamageService
    {
        void Damage(
            uint baseAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default,
            bool includeStats = true);

        void Heal(uint amount, IEntityBehaviour target);
    }

    public abstract class DamageServiceBase : IDamageService
    {
        protected abstract void OnDamage(
            uint fullAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default);

        protected abstract void OnHeal(
            uint amount, IEntityBehaviour target);

        public void Damage(
            uint baseAmount,
            IEntityBehaviour target,
            IEntityBehaviour from = null,
            HealthEventId eventId = default,
            bool includeStats = true)
        {
            if (target is not { IsValid: true }) return;

            if (includeStats == false)
            {
                OnDamage(baseAmount, target, from, eventId);
                return;
            }

            IStatOwnerBehaviour targetStat = target.GetBehaviour<IStatOwnerBehaviour>();
            IStatOwnerBehaviour fromStat = from?.GetBehaviour<IStatOwnerBehaviour>();

            float damage = fromStat?.GetStat(StatType.Damage, baseAmount) ?? baseAmount;
            float defense = targetStat?.GetStat(StatType.Defense, 0) ?? 0;

            float multiplier = targetStat?.GetDiminishingReturns(StatType.Defense, defense) ?? 1;
            float finalDamage = damage * multiplier;

            IItemOwnerBehaviour fromItemOwner = from?.GetBehaviour<IItemOwnerBehaviour>();

            if (fromItemOwner is not null)
            {
                foreach (ItemBehaviourBase behaviour in fromItemOwner.GetAllBehaviours())
                {
                    try { behaviour.OnDealDamage(target, fromItemOwner, ref finalDamage); }
                    catch (Exception ex) { Debug.LogException(ex); }
                }
            }

            OnDamage((uint)math.ceil(finalDamage), target, from, eventId);
        }

        public void Heal(uint amount, IEntityBehaviour target)
        {
            OnHeal(amount, target);
        }
    }
}
