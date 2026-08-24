using Tanks.Audio;
using Tanks.Client.Input;
using Tanks.Client.Rendering;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Driving;
using Unity.Mathematics;

namespace Tanks.Client.Player
{
    public sealed class EngineSound : NetworkComponent<ITankDriveBehaviour>
    {
        public EngineSound(
            IInputService<ManualDriveInput> input,
            IAudioFactory audioFactory,
            ICameraMainService cameraService)
        {
            _input = input;
            _audioFactory = audioFactory;
            _cameraService = cameraService;
        }

        private readonly IInputService<ManualDriveInput> _input;
        private readonly IAudioFactory _audioFactory;
        private readonly ICameraMainService _cameraService;

        private IAudioInstance _instance;
        private float2 _lastPosition;
        private PositiveFloatOrZero _intensity;

        protected override void OnInitialize()
        {
            _instance = _audioFactory.CreateInstance("event:/SFX/Engine/Engine_Default");
            _instance.SetVolume(0.5f);

            _instance.AttachTo(Behaviour.IsLocalPlayer ? _cameraService.Get().Transform : Transform);
            _instance.Play();
        }

        protected override void OnDestroy()
        {
            if (_instance is null) return;
            _instance.Stop(fadeOut: true);
            _instance.Dispose();
        }

        protected override void OnUpdate(float deltaTime)
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
                float2 currentPosition = Transform.WorldPosition;
                float distance = math.distance(currentPosition, _lastPosition);

                target = distance > deltaTime * 2 ? 1 : 0;
                _lastPosition = currentPosition;
            }

            _intensity = math.lerp(_intensity, target, math.saturate(deltaTime * speed));
            _instance.SetParameter("EngineIntensity", _intensity);
        }
    }
}
