using System.Collections.Generic;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Weapons.Projectiles.Modules
{
    public sealed class ImpactExplosionModule : RadialDamageModule,
        INetworkMessageHandler<ProjectileLaunched>
    {
        [SerializeField] private Dictionary<EffectType, float> _effects = new();

        public IDictionary<EffectType, float> Effects => _effects;
        private bool _exploded;

        public ImpactExplosionModule()
        {
            Radius = 5;
            NearDamage = 100;
            FarDamage = 40;
        }

        [Inject]
        public override void Construct(IDamageService service)
        {
            base.Construct(service);
            Projectile.RegisterMessageHandler(this);
        }

        public void Handle(ProjectileLaunched message) => _exploded = false;

        protected override void OnProjectileDone()
        {
            if (_exploded) return;
            _exploded = true;
            Explode(Projectile.Transform.WorldPosition);
        }

        protected override void BeforeDamage(IEntityBehaviour target, float distanceRatio)
        {
            if (Projectile.Owner.IsServer == false) return;

            IEffectOwnerBehaviour owner = target.GetBehaviour<IEffectOwnerBehaviour>();
            if (owner == null) return;

            foreach ((EffectType type, float duration) in _effects)
            {
                float scaledDuration = duration * (1 - distanceRatio);
                if (scaledDuration > 0)
                {
                    owner.ApplyEffect(type, scaledDuration, Projectile.Name, Projectile.Owner.Entity);
                }
            }
        }
    }
}
