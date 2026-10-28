using System.Linq;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;
using Unity.Mathematics;
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
                if (UnityEngine.Random.Range(0, count++) > 0) continue;
                chosenPlayer = player;
            }

            uint damage = (uint)Config.GetStat("damage").Value;
            HealthEventId eventId = HealthEventId.Create(GetType(), Time.time);

            int currentHealth = chosenPlayer
                .GetBehaviour<IHealthBehaviour>().ServerState.Value.Current;

            if (currentHealth <= 1) return;

            int remaining = (int)(currentHealth - damage);

            // Leave 1 HP if lethal
            if (remaining < 0) damage -= (uint)math.abs(remaining) + 1;

            // Don't count stats for pure damage
            _damageService.Damage(
                damage, chosenPlayer, owner.Entity, eventId, includeStats: false);
        }
    }
}
