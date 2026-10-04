using System.Linq;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Weapons;

namespace Tanks.Server.Items.Behaviours.OnFire
{
    [ItemBehaviour("true-balance")]
    public sealed class TrueBalanceBehaviour : ItemBehaviourBase
    {
        public TrueBalanceBehaviour(
            IEntityService entityService,
            IDamageService damageService)
        {
            _entityService = entityService;
            _damageService = damageService;
        }

        private readonly IEntityService _entityService;
        private readonly IDamageService _damageService;

        public override void OnFire(IItemOwnerBehaviour owner, WeaponUsed message)
        {
            ItemStat healStat = Config.GetStat("heal");

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                IItemOwnerBehaviour statOwner = entity.GetBehaviour<IItemOwnerBehaviour>();

                // Players with this item are not affected
                if (statOwner is null || statOwner.Items.Contains(Config.Identifier)) continue;

                _damageService.Heal(healStat.Value, entity);
            }
        }
    }
}
