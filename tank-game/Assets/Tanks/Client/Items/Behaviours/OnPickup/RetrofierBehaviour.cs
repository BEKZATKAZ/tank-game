using System.Linq;
using Tanks.Core.Settings;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;

namespace Tanks.Client.Items.Behaviours.OnPickup
{
    [ItemBehaviour("retrofier")]
    public sealed class RetrofierBehaviour : ItemBehaviourBase
    {
        public RetrofierBehaviour(IFrameRateLimitService frameRate) => _frameRate = frameRate;
        private readonly IFrameRateLimitService _frameRate;
        public override bool OncePerOwner => true;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (owner.IsLocalPlayer == false) return;
            int copies = owner.Items.Count(x => x == Config.Identifier);
            if (copies >= 3) _frameRate.RemoveLimit(owner);
            else _frameRate.SetLimit(owner, copies == 1 ? 20 : 15);
        }

        public override void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from, HealthState oldState, HealthState newState)
        {
            if (newState.Current <= 0) _frameRate.RemoveLimit(owner);
        }

        public override void OnDestroy(IItemOwnerBehaviour owner) => _frameRate.RemoveLimit(owner);
        public override void OnDespawn(IItemOwnerBehaviour owner) => _frameRate.RemoveLimit(owner);
    }
}
