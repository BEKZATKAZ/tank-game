using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Items.Behaviours.OnFire
{
    [ItemBehaviour("diminishing-returns")]
    public sealed class DiminishingReturnsBehaviour : ItemBehaviourBase
    {
        public override bool OncePerOwner => true;

        private readonly Dictionary<IItemOwnerBehaviour, int> _hits = new();

        public override void OnFire(IItemOwnerBehaviour owner, WeaponUsed message)
        {
            IHealthBehaviour health = owner.Entity.GetBehaviour<IHealthBehaviour>();
            if (health.ServerState.Value.Current <= Config.GetStat("threshold").Value) return;

            int copies = owner.Items.Count(x => x == Config.Identifier);

            owner.Entity.GetBehaviour<IEffectOwnerBehaviour>()?.ApplyEffect(
                EffectType.Burning, Config.GetStat("duration").Value * copies,
                Config.Identifier.ToString(), owner.Entity);
        }

        public override void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage)
        {
            if (target == owner.Entity || damage <= 0) return;
            if (target.GetBehaviour<ITeamParticipantBehaviour>()?.State == null) return;
            if (target.GetBehaviour<IHealthBehaviour>().ServerState.Value.Current <= 0) return;

            _hits.TryGetValue(owner, out int hits);
            _hits[owner] = (hits + 1) % 2;

            if (_hits[owner] != 0) return;

            int copies = owner.Items.Count(x => x == Config.Identifier);
            damage += damage * Config.GetStat("increase").Value * 0.01f * copies;
        }

        public override void OnDestroy(IItemOwnerBehaviour owner) => _hits.Remove(owner);
    }
}
