using Tanks.Shared.Driving;
using Tanks.Shared.Effects;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Player
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class TankExhaust : MonoBehaviour
    {
        private ITankDriveBehaviour _behaviour;
        private IEffectOwnerBehaviour _effectOwner;
        private ParticleSystem _particleSystem;
        private float2 _lastPosition;

        private void Start()
        {
            _behaviour = GetComponentInParent<ITankDriveBehaviour>();
            _effectOwner = GetComponentInParent<IEffectOwnerBehaviour>();

            _particleSystem = GetComponent<ParticleSystem>();
            _lastPosition = _behaviour.Transform.WorldPosition;
        }

        private void LateUpdate()
        {
            if (_effectOwner.Effects.ContainsKey(EffectType.Frozen))
            {
                if (_particleSystem.isPlaying) _particleSystem.Stop(withChildren: true);
                return;
            }

            if (_particleSystem.isStopped) _particleSystem.Play(withChildren: true);

            float2 currentPosition = _behaviour.Transform.WorldPosition;
            float distance = math.distance(currentPosition, _lastPosition);

            bool isMoving = distance > 0.01f;
            _lastPosition = currentPosition;

            var emission = _particleSystem.emission;
            emission.rateOverTime = isMoving ? 10 : 4;
        }
    }
}
