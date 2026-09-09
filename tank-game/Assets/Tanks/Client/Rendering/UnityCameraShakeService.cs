using NaughtyAttributes;
using Tanks.Shared.Adapters;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

namespace Tanks.Client.Rendering
{
    public sealed class UnityCameraShakeService : MonoBehaviour, ICameraShakeService
    {
        [CurveRange(0, 0, 1, 1)]
        [SerializeField]
        private AnimationCurve _falloff = new AnimationCurve(
            new Keyframe(0, 0),
            new Keyframe(0.1f, 1, 0, -3),
            new Keyframe(1, 0));

        private ICameraMainService _cameraMain;
        private NativeList<ActiveCameraImpulse> _activeImpulses;

        [Inject]
        public void Inject(ICameraMainService cameraMain)
        {
            _cameraMain = cameraMain;
        }

        public void AddShake(in CameraImpulse impulse)
        {
            _activeImpulses.Add(new ActiveCameraImpulse(impulse, Time.time));
        }

        private void Awake()
        {
            _activeImpulses = new NativeList<ActiveCameraImpulse>(8, Allocator.Persistent);
        }

        private void OnDestroy()
        {
            if (_activeImpulses.IsCreated) _activeImpulses.Dispose();
        }

        private void LateUpdate()
        {
            ICamera camera = _cameraMain.Get();
            if (camera is null) return;

            float2 cameraPosition = camera.BodyTransform.WorldPosition;
            float now = Time.time;

            camera.ShakeTransform.LocalPosition = float2.zero;
            camera.ShakeTransform.LocalRotation = default;

            foreach (ActiveCameraImpulse impulse in _activeImpulses)
            {
                float distance = math.distance(cameraPosition, impulse.Impulse.Position);
                float normalizedDistance = math.saturate(distance / impulse.Impulse.Radius);
                float distanceFalloff = 1 - normalizedDistance;

                if (distanceFalloff <= 0) continue;

                float duration = impulse.Impulse.Duration;
                float frequency = impulse.Impulse.Frequency;

                float elapsed = now - impulse.StartTime;
                float normalizedTime = math.saturate(elapsed / duration);

                float falloff = _falloff.Evaluate(normalizedTime);
                float amplitude = impulse.Impulse.Amplitude * falloff * distanceFalloff;
                float noiseTime = elapsed * frequency + impulse.StartTime;

                float x = Mathf.PerlinNoise1D(noiseTime) - 0.5f;
                float y = Mathf.PerlinNoise1D(noiseTime + 100) - 0.5f;
                float z = Mathf.PerlinNoise1D(noiseTime + 200) - 0.5f;

                camera.ShakeTransform.LocalPosition += new float2(x, y) * amplitude;
                camera.ShakeTransform.LocalRotation += Angle.FromDegrees(amplitude * z);
            }

        backtrack:

            for (int i = 0; i < _activeImpulses.Length; i++)
            {
                ActiveCameraImpulse impulse = _activeImpulses[i];

                float elapsed = now - impulse.StartTime;
                if (elapsed < impulse.Impulse.Duration) continue;

                _activeImpulses.RemoveAt(i);
                goto backtrack;
            }
        }
    }
}
