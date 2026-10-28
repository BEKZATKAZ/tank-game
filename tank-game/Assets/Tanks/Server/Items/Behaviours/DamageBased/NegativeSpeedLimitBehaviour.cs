using System.Collections.Generic;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Unity.Mathematics;

namespace Tanks.Server.Items.Behaviours.DamageBased
{
    public class PlayerNegativeSpeedEntry
    {
        public float2 LastPosition;
        public bool ShouldDamage;
    }

    [ItemBehaviour("negative-speed-limit")]
    public sealed class NegativeSpeedLimitBehaviour : ItemBehaviourBase
    {
        public NegativeSpeedLimitBehaviour(IDamageService damageService)
        {
            _damageService = damageService;
        }

        private readonly IDamageService _damageService;
        private readonly Dictionary<IItemOwnerBehaviour, PlayerNegativeSpeedEntry> _entries = new();

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_entries.ContainsKey(owner)) return;

            _entries[owner] = new PlayerNegativeSpeedEntry
            {
                LastPosition = owner.Transform.WorldPosition
            };
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            _entries.Remove(owner);
        }

        public override void OnTick(IItemOwnerBehaviour owner, int tick, float tickDelta)
        {
            if (_entries.ContainsKey(owner) == false) return;

            PlayerNegativeSpeedEntry entry = Calculate(owner);

            if (tick % 5 != 0) return;

            if (entry.ShouldDamage)
            {
                uint damage = (uint)Config.GetStat("damage").Value;
                HealthEventId eventId = HealthEventId.Create(GetType(), tick);

                _damageService.Damage(
                    damage, owner.Entity, owner.Entity, eventId, includeStats: false);
            }

            entry.ShouldDamage = false;
        }

        private PlayerNegativeSpeedEntry Calculate(IItemOwnerBehaviour owner)
        {
            PlayerNegativeSpeedEntry entry = _entries[owner];
            float2 currentPosition = owner.Transform.WorldPosition;

            float2 difference = currentPosition - entry.LastPosition;
            entry.LastPosition = currentPosition;
            float length = math.length(difference);

            // Prevent taking damage on telepors or little movement
            if (length <= 0.2f || length > 10) return entry;

            float2 direction = math.normalizesafe(difference);
            float dot = math.dot(direction, owner.Transform.Right);

            // Only damage forward movement
            if (dot <= 0.75f) return entry;

            entry.ShouldDamage = true;
            return entry;
        }
    }
}
