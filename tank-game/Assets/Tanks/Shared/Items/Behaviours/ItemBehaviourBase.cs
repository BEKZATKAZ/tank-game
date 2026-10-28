using System;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Weapons;

namespace Tanks.Shared.Items.Behaviours
{
    public abstract class ItemBehaviourBase
    {
        protected IItemConfig Config { get; private set; }

        public virtual void Initialize(IItemConfig config) => Config = config;
        public virtual void OnTick(IItemOwnerBehaviour owner, int tick, float tickDelta) { }
        public virtual void OnDestroy(IItemOwnerBehaviour owner) { }
        public virtual void OnPickup(IItemOwnerBehaviour owner) { }
        public virtual void OnFire(IItemOwnerBehaviour owner, WeaponUsed message) { }

        public virtual void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage) { }
        public virtual void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from,
            HealthState oldState, HealthState newState) { }
    }
}
