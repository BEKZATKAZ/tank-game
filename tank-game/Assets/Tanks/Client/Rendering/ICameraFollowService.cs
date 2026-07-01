using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Rendering
{
    public interface ICameraFollowService
    {
        void SetDefault(float2 defaultPosition, float defaultOrthographicSize, bool forceOverride);

        void SetFollowTarget(ITransform target, float orthographicSize);
    }

    public sealed class CameraFollowService : MonoBehaviour, ICameraFollowService
    {
        [Inject] private readonly ICameraMainService _cameraMain;

        private ITransform _target;
        private float _targetOrthographicSize;

        private float2 _defaultPosition;
        private float _defaultOrthographicSize = 5;

        public void SetDefault(
            float2 defaultPosition, float defaultOrthographicSize, bool forceOverride)
        {
            _defaultPosition = defaultPosition;
            _defaultOrthographicSize = defaultOrthographicSize;

            if (forceOverride == false) return;

            ICamera camera = _cameraMain.Get();
            camera.Transform.WorldPosition = defaultPosition;
        }

        public void SetFollowTarget(ITransform target, float orthographicSize)
        {
            _target = target;
            _targetOrthographicSize = orthographicSize;
        }

        private void LerpTo(ICamera camera, float2 target, float size)
        {
            float t = 5 * Time.deltaTime;
            float2 position = math.lerp(camera.Transform.WorldPosition, target, t);

            camera.Transform.WorldPosition = new float2(position.x, position.y);
            camera.OrthographicSize = math.lerp(camera.OrthographicSize, size, t);
        }

        private void LateUpdate()
        {
            ICamera camera = _cameraMain.Get();

            if (_target is null)
            {
                LerpTo(camera, _defaultPosition, _defaultOrthographicSize);
                return;
            }

            LerpTo(camera, _target.WorldPosition, _targetOrthographicSize);
        }
    }
}