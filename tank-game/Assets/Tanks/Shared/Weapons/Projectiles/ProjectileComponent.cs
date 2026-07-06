using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Health;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Weapons.Projectiles
{
    public class ProjectileComponent : MonoBehaviour, IProjectile, IDamageDealer
    {
        public ITransform Transform { get; private set; }
        public LaunchSettings Settings { get; private set; }

        private LaunchCallbacks _callbacks;
        private PositiveFloatOrZero _startTime;
        private int _richochetedTimes;

        public void Launch(LaunchSettings settings, LaunchCallbacks callbacks)
        {
            Settings = settings;
            _callbacks = callbacks;
            _startTime = Time.time;
            _richochetedTimes = 0;
        }

        private void Awake()
        {
            Transform = new UnityTransform(transform);
        }

        private void Update()
        {
            PositiveFloatOrZero elapsed = Time.time - _startTime;

            if (elapsed >= Settings.LifeTimeSeconds)
            {
                _callbacks.OnDone?.Invoke(this);
                return;
            }

            float deltaTime = Time.deltaTime;
            int mask = (1 << 0) | (1 << 3); // Only walls & entities

            float2 position = Transform.WorldPosition;
            float radius = math.cmax(Transform.WorldScale) * 0.5f;
            float distance = Settings.Speed * deltaTime;

            RaycastHit2D hit = Physics2D.Raycast(position, Transform.Right, radius + distance, mask);

            if (hit.collider == false)
            {
                Transform.WorldPosition += Transform.Right * distance;
                return;
            }

            if (hit.collider.TryGetComponent(out IDamageable damageable))
            {
                float damage = Settings.Damage.Evaluate(elapsed);
                damageable.Damage(damage, this);

                _callbacks.OnDone?.Invoke(this);
                return;
            }

            if (++_richochetedTimes > Settings.RicochetCount)
            {
                _callbacks.OnDone?.Invoke(this);
                return;
            }

            float2 normal = hit.normal;
            float2 result = Transform.WorldPosition;

            // Move to the collision point
            float travel = math.max(0, hit.distance - radius);
            result += Transform.Right * travel;

            float2 reflected = math.reflect(Transform.Right, normal);
            Transform.WorldRotation = Angle.FromRadians(math.atan2(reflected.y, reflected.x));
            result += normal * 0.001f; // Push slightly away from the wall

            float remaining = math.max(0, distance - travel);
            result += Transform.Right * remaining;
            Transform.WorldPosition = result;
        }
    }
}