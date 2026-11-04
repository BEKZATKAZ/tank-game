using System.Collections.Generic;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Projectiles.Modules;
using UnityEngine;

namespace Tanks.Server.Weapons.Projectiles.Modules
{
    public sealed class GiveEffectModule : ProjectileModuleBehaviourBase
    {
        [SerializeField] private Dictionary<EffectType, float> _effects = new();

        public IDictionary<EffectType, float> Effects => _effects;

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            IEffectOwnerBehaviour effectOwner = target.GetBehaviour<IEffectOwnerBehaviour>();
            if (effectOwner is null) return;

            foreach ((EffectType type, float duration) in _effects)
            {
                effectOwner.AddEffect(type, duration, from);
            }
        }
    }
}
