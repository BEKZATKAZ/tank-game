using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons.Projectiles;
using Tanks.Shared.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Server.Weapons.Projectiles.Modules
{
    public sealed class AreaEffectModule : ProjectileModuleBehaviourBase,
        INetworkMessageHandler<ProjectileLaunched>
    {
        public float Radius = 10;
        public bool DistanceFalloff = true;
        public bool RestartEffects = true;
        public LayerMask TargetMask = 1 << 3;
        public LayerMask ObstacleMask = 1;

        [SerializeField] private Dictionary<EffectType, float> _effects = new();

        public IDictionary<EffectType, float> Effects => _effects;

        private readonly List<Collider2D> _colliders = new();
        private readonly List<RaycastHit2D> _obstacles = new();
        private readonly HashSet<IEntityBehaviour> _targets = new();
        private bool _triggered;

        [Inject]
        public void Construct()
        {
            Projectile.RegisterMessageHandler(this);
        }

        public void Handle(ProjectileLaunched message) => _triggered = false;

        protected override void OnProjectileDone()
        {
            float2 center = Projectile.Transform.WorldPosition;

            if (_triggered || Projectile.Owner.IsServer == false) return;
            _triggered = true;
            _targets.Clear();

            if (Radius <= 0 || _effects.Count == 0) return;

            Physics2D.OverlapCircle(center, Radius, new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = TargetMask,
                useTriggers = false
            }, _colliders);

            foreach (Collider2D collider in _colliders)
            {
                if (collider.TryGetComponent(out IHealthBehaviour health) == false) continue;
                IEntityBehaviour target = health.Entity;
                if (_targets.Contains(target) || health.ServerState.Value.Current <= 0) continue;

                float distance = math.distance(target.Transform.WorldPosition, center);
                if (distance >= Radius) continue;

                float2 delta = (float2)collider.ClosestPoint(center) - center;
                float rayLength = math.length(delta);

                if (rayLength > 0.001f && Physics2D.Raycast(center, delta / rayLength,
                    new ContactFilter2D
                    {
                        useLayerMask = true,
                        layerMask = ObstacleMask,
                        useTriggers = false
                    }, _obstacles, rayLength) > 0) continue;

                IEffectOwnerBehaviour owner = target.GetBehaviour<IEffectOwnerBehaviour>();
                if (owner is null) continue;
                _targets.Add(target);

                float strength = DistanceFalloff ? math.saturate(1 - distance / Radius) : 1;

                foreach ((EffectType type, float duration) in _effects)
                {
                    float seconds = duration * strength;
                    if (seconds <= 0) continue;

                    owner.ApplyEffect(type, seconds, Projectile.Name, Projectile.Owner.Entity, RestartEffects);
                }
            }
        }
    }
}
