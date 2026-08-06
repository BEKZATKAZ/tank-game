using Tanks.Audio;
using Tanks.Client.Input;
using Tanks.Client.Rendering;
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
        [Inject] private readonly IAudioFactory _audioFactory;
        [Inject] private readonly ICameraMainService _cameraService;

        private IAudioInstance _instance;
        private float2 _lastPosition;
        private PositiveFloatOrZero _intensity;

        private void Start()
        {
            _instance = _audioFactory.CreateInstance("event:/SFX/Engine/Engine_Default");
            _instance.SetVolume(0.5f);

            _instance.AttachTo(Behaviour.IsLocalPlayer
                ? _cameraService.Get().Transform
                : Behaviour.Transform);

            _instance.Play();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (_instance is null) return;

            _instance.Stop(fadeOut: true);
            _instance.Dispose();
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
            _instance.SetParameter("EngineIntensity", _intensity);
        }
    }
}
