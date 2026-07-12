using FMOD.Studio;
using FMODUnity;
using Tanks.Client.Input;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Driving;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Player
{
    public sealed class EngineSound : NetworkComponent<TankDriveBehaviour>
    {
        [Inject] private readonly IInputService<ManualDriveInput> _input;

        private EventInstance _instance;
        private float2 _lastPosition;
        private PositiveFloatOrZero _intensity;

        private void Start()
        {
            _instance = RuntimeManager.CreateInstance("event:/SFX/Engine/Engine_Default");
            _instance.setVolume(0.5f);

            RuntimeManager.AttachInstanceToGameObject(_instance, gameObject);
            _instance.start();
        }

        private void Update()
        {
            PositiveFloatOrZero target;
            float speed = 2;

            if (Behaviour.IsLocalPlayer)
            {
                ManualDriveInput input = _input.Read();
                target = math.abs(input.Throttle);
            }
            else
            {
                float2 currentPosition = Behaviour.Transform.WorldPosition;
                float distance = math.distance(currentPosition, _lastPosition);

                target = distance > Time.deltaTime * 2 ? 1 : 0;
                _lastPosition = currentPosition;
            }

            _intensity = math.lerp(_intensity, target, Time.deltaTime * speed);
            _instance.setParameterByName("EngineIntensity", _intensity);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _instance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            _instance.release();
        }
    }
}
