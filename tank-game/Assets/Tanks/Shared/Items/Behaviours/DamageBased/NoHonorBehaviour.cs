using System.Linq;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Shared.Items.Behaviours.DamageBased
{
    [ItemBehaviour("no-honor")]
    public sealed class NoHonorBehaviour : ItemBehaviourBase
    {
        public override bool OncePerOwner => true;

        public override void OnDirectHit(IItemOwnerBehaviour owner, ref DirectHitContext context)
        {
            float alignment = math.dot(math.normalizesafe(context.Direction), context.Target.Transform.Right);
            if (alignment < math.cos(math.radians(45))) return;

            int copies = owner.Items.Count(x => x == Config.Identifier);
            context.DamageMultiplier += Config.GetStat("increase").Value * 0.01f * copies;
        }
    }
}
