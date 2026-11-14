using NaughtyAttributes;
using System.Collections.Generic;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared.Adapters;
using Tanks.Shared.Effects;
using Tanks.Shared.Stats;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Driving
{
    public interface ITankDriveBehaviour : INetworkBehaviourExtended
    {
        float DefaultSpeed { get; }
        float TurnRate { get; }

        void EmitTireTracks(bool left, bool right);
        void Tick(float throttle, float steering, float deltaTime);
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(StatOwnerBehaviour), typeof(EffectOwnerBehaviour))]
    public sealed class TankDriveBehaviour : NetworkBehaviourExtended, ITankDriveBehaviour
    {
        [Required]
        [SerializeField]
        private TrailRenderer _leftTire;

        [Required]
        [SerializeField]
        private TrailRenderer _rightTire;

        [Inject]
        private readonly ISessionSettingsService _settings;

        private readonly List<RaycastHit2D> _hitBuffer = new();
        private IStatOwnerBehaviour _statOwner;
        private IEffectOwnerBehaviour _effectOwner;
        private ArcadeDriver _driver;

        public float DefaultSpeed { get; private set; }
        public float TurnRate => _driver?.TurnRate ?? 0;
        public Rigidbody2D Rigidbody { get; private set; }
        public IVelocity Velocity { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Rigidbody = GetComponent<Rigidbody2D>();
            Velocity = new UnityVelocity(Rigidbody);

            _statOwner = GetComponent<StatOwnerBehaviour>();
            _effectOwner = GetComponent<EffectOwnerBehaviour>();
        }

        protected override void OnInitialize()
        {
            DefaultSpeed = _settings.GetSettings()?.Stats.ForwardMovementSpeed ?? 10;
            int backwardsSpeed = _settings.GetSettings()?.Stats.BackwardsMovementSpeed ?? 8;

            _driver = new ArcadeDriver(new ArcadeDriverSettings
            {
                AccelerationTime = 0.3f,
                MaxSpeed = (byte)DefaultSpeed,
                ReverseSpeed = (byte)backwardsSpeed,
                SteeringSpeed = Angle.FromDegrees(240)
            });
        }

        public void EmitTireTracks(bool left, bool right)
        {
            _leftTire.emitting = left;
            _rightTire.emitting = right;
        }

        public void Tick(float throttle, float steering, float deltaTime)
        {
            if (_effectOwner.Effects.ContainsKey(EffectType.Frozen))
            {
                _driver.SetMaxAcceleration(0);
                Velocity.Linear = default;
                Velocity.Angular = default;
                return;
            }

            Velocity.Angular = _driver.Steer(steering);

            float2 linear = _driver.Drive(Transform.Right, throttle, deltaTime);
            float length = math.length(linear);

            if (length < 0.01f)
            {
                Velocity.Linear = default;
                return;
            }
            
            // Apply stats
            float newLength = _statOwner.GetStat(StatType.Speed, baseValue: length);
            linear = math.normalize(linear) * newLength;

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _effectOwner.Effects.ContainsKey(EffectType.Noclip)
                    ? ~1 : -1,
                useTriggers = false
            };

            float2 direction = math.normalizesafe(linear);
            float distance = math.length(linear) * deltaTime;

            if (TryGetBlockingNormal(direction, filter, distance, out float2 normal))
            {
                float minDrop = 0.5f;
                float drop = 1 - math.abs(math.dot(direction, normal));

                linear -= math.dot(linear, normal) * normal;
                _driver.SetMaxAcceleration(math.max(drop, minDrop));
            }

            Velocity.Linear = linear;
        }

        private bool TryGetBlockingNormal(
            float2 direction,
            ContactFilter2D filter,
            float distance,
            out float2 normal)
        {
            Rigidbody.Cast(direction, filter, _hitBuffer, distance);

            float nearest = float.PositiveInfinity;
            normal = default;

            foreach (RaycastHit2D hit in _hitBuffer)
            {
                if (hit.distance >= nearest) continue;
                if (math.dot(direction, (float2)hit.normal) >= 0) continue;

                nearest = hit.distance;
                normal = hit.normal;
            }

            return nearest < float.PositiveInfinity;
        }
    }
}
