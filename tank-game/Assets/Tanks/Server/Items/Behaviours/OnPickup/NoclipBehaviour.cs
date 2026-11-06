using System;
using System.Collections.Generic;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Items.Behaviours.OnPickup
{
    [ItemBehaviour("noclip")]
    public sealed class NoclipBehaviour : ItemBehaviourBase
    {
        private sealed class PlayerEntry
        {
            public IEffectOwnerBehaviour Effects;
            public Action<EffectType, EffectEndReason> Handler;
        }

        public NoclipBehaviour(IDamageService damage) => _damage = damage;

        private readonly IDamageService _damage;
        private readonly Dictionary<IItemOwnerBehaviour, PlayerEntry> _entries = new();
        private readonly Collider2D[] _wallBuffer = new Collider2D[1];
        public override bool OncePerOwner => true;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_entries.TryGetValue(owner, out PlayerEntry entry) == false)
            {
                entry = new PlayerEntry
                {
                    Effects = owner.Entity.GetBehaviour<IEffectOwnerBehaviour>()
                };

                if (entry.Effects == null) return;

                entry.Handler = (type, reason) =>
                {
                    if (type != EffectType.Noclip || reason != EffectEndReason.Expired) return;

                    EliminateIfInsideWall(owner);
                };

                entry.Effects.Ended += entry.Handler;
                _entries.Add(owner, entry);
            }

            entry.Effects.ApplyEffect(
                EffectType.Noclip,
                Config.GetStat("duration").Value,
                Config.Identifier.ToString(), owner.Entity);
        }

        private void EliminateIfInsideWall(IItemOwnerBehaviour owner)
        {
            IEntityBehaviour entity = owner.Entity;

            float2 size = math.max(
                entity.Size * math.abs(entity.Transform.WorldScale), 0.001f) * 0.5f;

            int count = Physics2D.OverlapBox(
                entity.Transform.WorldPosition, size, entity.Transform.WorldRotation.Degrees,
                new ContactFilter2D
                {
                    useLayerMask = true,
                    layerMask = 1,
                    useTriggers = false
                }, _wallBuffer);

            if (count == 0) return;

            int health = entity.GetBehaviour<IHealthBehaviour>().ServerState.Value.Current;

            if (health > 0) _damage.Damage((uint)health, entity, includeStats: false);
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            if (_entries.Remove(owner, out PlayerEntry entry) == false) return;

            entry.Effects.Ended -= entry.Handler;
        }
    }
}
