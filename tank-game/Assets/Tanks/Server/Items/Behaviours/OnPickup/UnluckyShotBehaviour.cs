using System.Linq;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;
using UnityEngine;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("unlucky-shot")]
    public sealed class UnluckyShotBehaviour : ItemBehaviourBase
    {
        public UnluckyShotBehaviour(
            IEntityService entityService, 
            IDamageService damageService)
        {
            _entityService = entityService;
            _damageService = damageService;
        }

        private readonly IEntityService _entityService;
        private readonly IDamageService _damageService;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            var alivePlayers = _entityService
                .GetAllEntities()
                .Where(x => x.IsValid && x.GetBehaviour<ITeamParticipantBehaviour>() != null);

            IEntityBehaviour chosenPlayer = null;
            int count = 0;

            foreach (IEntityBehaviour player in alivePlayers)
            {
                if (Random.Range(0, count++) > 0) continue;
                chosenPlayer = player;
            }

            float damage = Config.GetStat("damage").Value;
            HealthEventId eventId = HealthEventId.Create(GetType(), Time.time);

            // Don't count stats for pure damage
            _damageService.Damage(
                damage, chosenPlayer, owner.Entity, eventId, includeStats: false);
        }
    }
}
