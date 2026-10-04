using System.Linq;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("missing-texture")]
    public sealed class MissingTextureBehaviour : ItemBehaviourBase
    {
        public MissingTextureBehaviour(IDamageService damageService)
        {
            _damageService = damageService;
        }

        private readonly IDamageService _damageService;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            int count = owner.Items.Count(x => x == Config.Identifier);
            if (count < 2) return;
            _damageService.Damage(float.MaxValue, owner.Entity, owner.Entity, includeStats: false);
        }
    }
}
