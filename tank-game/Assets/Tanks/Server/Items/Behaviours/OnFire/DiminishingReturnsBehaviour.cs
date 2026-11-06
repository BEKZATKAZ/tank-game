using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Items.Behaviours.OnFire
{
    [ItemBehaviour("diminishing-returns")]
    public sealed class DiminishingReturnsBehaviour : ItemBehaviourBase
    {
        private readonly Dictionary<IItemOwnerBehaviour, int> _hits = new();
        public override bool OncePerOwner => true;

        public override void OnFire(IItemOwnerBehaviour owner, WeaponUsed message)
        {
            IHealthBehaviour health = owner.Entity.GetBehaviour<IHealthBehaviour>();
            if (health.ServerState.Value.Current <= Config.GetStat("threshold").Value) return;

            int copies = owner.Items.Count(x => x == Config.Identifier);

            owner.Entity.GetBehaviour<IEffectOwnerBehaviour>()?.ApplyEffect(
                EffectType.Burning, Config.GetStat("duration").Value * copies,
                Config.Identifier.ToString(), owner.Entity);
        }

        public override void OnDirectHit(IItemOwnerBehaviour owner, ref DirectHitContext context)
        {
            _hits.TryGetValue(owner, out int hits);
            _hits[owner] = hits + 1;

            if ((hits + 1) % 2 != 0) return;

            int copies = owner.Items.Count(x => x == Config.Identifier);
            context.DamageMultiplier += Config.GetStat("increase").Value * 0.01f * copies;
        }

        public override void OnDestroy(IItemOwnerBehaviour owner) => _hits.Remove(owner);
    }
}
