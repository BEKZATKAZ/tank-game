using System.Collections.Generic;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Weapons.Projectiles.Modules;
using UnityEngine;

namespace Tanks.Server.Weapons.Projectiles.Modules
{
    public sealed class GiveEffectModule : ProjectileModuleBehaviourBase
    {
        public bool RestartEffects;

        [SerializeField] private Dictionary<EffectType, float> _effects = new();

        public IDictionary<EffectType, float> Effects => _effects;

        protected override void OnHitEntity(IEntityBehaviour target, IEntityBehaviour from)
        {
            if (Projectile.Owner.IsServer == false) return;

            IEffectOwnerBehaviour effectOwner = target.GetBehaviour<IEffectOwnerBehaviour>();
            if (effectOwner is null) return;

            foreach ((EffectType type, float duration) in _effects)
            {
                effectOwner.ApplyEffect(type, duration, Projectile.Name, from, RestartEffects);
            }
        }
    }
}
