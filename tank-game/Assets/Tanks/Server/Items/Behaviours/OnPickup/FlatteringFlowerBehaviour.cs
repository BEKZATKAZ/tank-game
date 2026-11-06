using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Teams;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("flattering-flower")]
    public sealed class FlatteringFlowerBehaviour : ItemBehaviourBase
    {
        public FlatteringFlowerBehaviour(IEntityService entities) => _entities = entities;

        private readonly IEntityService _entities;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            float duration = Config.GetStat("duration").Value;
            string producer = Config.Identifier.ToString();

            foreach (IEntityBehaviour entity in _entities.GetAllEntities())
            {
                if (entity.GetBehaviour<ITeamParticipantBehaviour>()?.State == null) continue;
                if (entity.GetBehaviour<IHealthBehaviour>()?.ServerState.Value.Current is not > 0)
                {
                    continue;
                }

                IEffectOwnerBehaviour effectOwner = entity
                    .GetBehaviour<IEffectOwnerBehaviour>();

                effectOwner.ApplyEffect(EffectType.Healing, duration, producer, owner.Entity);
            }
        }
    }
}
