using System.Linq;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Stats;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("retrofier")]
    public sealed class RetrofierBehaviour : ItemBehaviourBase
    {
        public RetrofierBehaviour(IDamageService damage) => _damage = damage;

        private readonly IDamageService _damage;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            int copies = owner.Items.Count(x => x == Config.Identifier);

            if (copies >= 3)
            {
                int health = owner.Entity
                    .GetBehaviour<IHealthBehaviour>().ServerState.Value.Current;

                if (health > 0) _damage.Damage((uint)health, owner.Entity, includeStats: false);

                return;
            }

            owner.Entity.GetBehaviour<IStatOwnerBehaviour>()?.AddStat(
                copies == 1 ? StatType.Defense : StatType.Damage,
                StatValueType.Flat, Config.GetStat(copies == 1 ? "first" : "second").Value);
        }
    }
}
