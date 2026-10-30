using NaughtyAttributes;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared.Adapters;
using Tanks.Shared.Stats;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Driving
{
    public interface ITankDriveBehaviour : INetworkBehaviourExtended
    {
        float DefaultSpeed { get; }

        void EmitTireTracks(bool left, bool right);
        void Tick(float throttle, float steering, float deltaTime);
    }

    [RequireComponent(typeof(Rigidbody2D), typeof(StatOwnerBehaviour))]
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

        private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[1];
        private StatOwnerBehaviour _statOwner;
        private ArcadeDriver _driver;

        public float DefaultSpeed { get; private set; }
        public Rigidbody2D Rigidbody { get; private set; }
        public IVelocity Velocity { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Rigidbody = GetComponent<Rigidbody2D>();
            Velocity = new UnityVelocity(Rigidbody);
            _statOwner = GetComponent<StatOwnerBehaviour>();
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
                layerMask = ~(1 << gameObject.layer),
                useTriggers = false
            };

            float2 direction = math.normalizesafe(linear);
            float distance = math.length(linear) * deltaTime;

            if (Rigidbody.Cast(direction, filter, _hitBuffer, distance) > 0)
            {
                float2 normal = _hitBuffer[0].normal;
                float minDrop = 0.5f;
                float drop = 1 - math.abs(math.dot(direction, normal));

                linear -= math.dot(linear, normal) * normal;
                _driver.SetMaxAcceleration(math.max(drop, minDrop));
            }

            Velocity.Linear = linear;
        }
    }
}
