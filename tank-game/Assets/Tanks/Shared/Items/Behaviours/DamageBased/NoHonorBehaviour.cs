using System.Linq;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Teams;
using Unity.Mathematics;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("no-honor")]
    public sealed class NoHonorBehaviour : ItemBehaviourBase
    {
        public override bool OncePerOwner => true;

        public override void OnDealDamage(
            IEntityBehaviour target, IItemOwnerBehaviour owner, ref float damage)
        {
            if (target == owner.Entity || damage <= 0) return;
            if (target.GetBehaviour<ITeamParticipantBehaviour>()?.State == null) return;
            if (target.GetBehaviour<IHealthBehaviour>().ServerState.Value.Current <= 0) return;

            float2 direction = target.Transform.WorldPosition - owner.Entity.Transform.WorldPosition;
            float alignment = math.dot(math.normalizesafe(direction), target.Transform.Right);
            if (alignment < math.cos(math.radians(45))) return;

            int copies = owner.Items.Count(x => x == Config.Identifier);
            damage += damage * Config.GetStat("increase").Value * 0.01f * copies;
        }
    }
}
