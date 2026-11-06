using FMODUnity;
using Tanks.Shared.Adapters;
using Tanks.UI.TankModel;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Menus.TankModel
{
    public sealed class TankModelEngineSound : MonoBehaviour
    {
        [SerializeField] private StudioEventEmitter _sound;

        private ITankModelService _service;
        private ITransform _transform;
        private float2 _lastPosition;
        private float _intensity;
        private bool _isLocalPlayer;

        public void Initialize(ITankModelService service, int index)
        {
            _service = service;
            _isLocalPlayer = index == 0;
        }

        private void Start()
        {
            _transform = new UnityTransform(transform);
            _lastPosition = _transform.WorldPosition;
            _sound.EventInstance.setVolume(0.5f);
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            float speed = 0.75f;

            float2 currentPosition = _transform.WorldPosition;
            float distance = math.distance(currentPosition, _lastPosition);
            float target = _service.IsDrivingInPlace || distance > deltaTime * 4 ? 1 : 0;

            _lastPosition = currentPosition;
            _intensity = math.lerp(_intensity, target, math.saturate(deltaTime * speed));
            _sound.SetParameter("EngineIntensity", _intensity);

            if (_isLocalPlayer) return;
            _sound.EventInstance.setVolume(_intensity);
        }
    }
}
