using System.Linq;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;
using UnityEngine;

namespace Tanks.Client.Items.Behaviours.OnPickup
{
    [ItemBehaviour("missing-texture")]
    public sealed class MissingTextureBehaviour : ItemBehaviourBase
    {
        public MissingTextureBehaviour(IEntityService entityService)
        {
            _entityService = entityService;
        }

        private readonly IEntityService _entityService;

        private void SetSpriteVisibility(IEntityBehaviour entity, bool isVisible)
        {
            IHealthBehaviour health = entity.GetBehaviour<IHealthBehaviour>();

            if (health is null) return;

            foreach (SpriteRenderer sprite in health.GetAllSpriteRenderers())
            {
                sprite.enabled = isVisible;
            }
        }

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (owner.IsLocalPlayer == false)
            {
                SetSpriteVisibility(owner.Entity, isVisible: false);
                return;
            }

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                // Ignore crates and other non-player entities
                if (entity.GetBehaviour<ITeamParticipantBehaviour>() is null) continue;

                SetSpriteVisibility(entity, isVisible: false);
            }
        }

        public override void OnHealthChanged(
            IItemOwnerBehaviour owner, IEntityBehaviour from,
            HealthState oldState, HealthState newState)
        {
            if (owner.IsLocalPlayer == false) return;
            if (newState.Current > 0) return;

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                // Ignore crates and other non-player entities
                if (entity.GetBehaviour<ITeamParticipantBehaviour>() is null) continue;

                IItemOwnerBehaviour itemOwner = entity.GetBehaviour<IItemOwnerBehaviour>();

                if (itemOwner is null) continue;
                if (itemOwner.Items.Contains(Config.Identifier)) continue;

                IHealthBehaviour health = entity.GetBehaviour<IHealthBehaviour>();
                if (health is null) continue;

                foreach (SpriteRenderer sprite in health.GetAllSpriteRenderers())
                {
                    sprite.enabled = true;
                }
            }
        }
    }
}
