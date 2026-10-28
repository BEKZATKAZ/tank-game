using System.Collections.Generic;
using Tanks.Shared.Entities;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("lucky-shot")]
    public sealed class LuckyShotBehaviour : ItemBehaviourBase
    {
        private readonly Dictionary<IItemOwnerBehaviour, List<byte>> _hits = new();

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_hits.TryGetValue(owner, out List<byte> hits) == false)
            {
                hits = new List<byte>();
                _hits[owner] = hits;
            }

            hits.Add(0);
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            if (_hits.TryGetValue(owner, out List<byte> hits) == false) return;

            hits.RemoveAt(0);
            if (hits.Count == 0) _hits.Remove(owner);
        }

        public override void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage)
        {
            if (_hits.TryGetValue(owner, out List<byte> hits) == false) return;

            // Damage dispatch invokes this once per copy, so advance one counter per call.
            byte count = (byte)((hits[0] + 1) % 3);
            hits.RemoveAt(0);
            hits.Add(count);
            if (count != 0) return;

            float multiplier = Config.GetStat("damage").Value * 0.01f;
            damage += damage * multiplier;
        }
    }
}
