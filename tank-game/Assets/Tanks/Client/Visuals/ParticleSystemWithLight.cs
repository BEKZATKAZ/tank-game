using LitMotion;
using NaughtyAttributes;
using Tanks.Shared.Adapters;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tanks.Client.Visuals
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(ParticleSystem), typeof(Light2D))]
    public class ParticleSystemWithLight : MonoBehaviour
    {
        [SerializeField]
        private float _lightIntensity = 20;

        [CurveRange(0, 0, 1, 1)]
        [SerializeField]
        private AnimationCurve _curve = AnimationCurve.Linear(0, 1, 1, 0);

        public ITransform Transform { get; private set; }

        private ParticleSystem _system;
        private Light2D _light;
        private MotionHandle _animation;
        private ITransform _stickedTo;

        private void Awake()
        {
            Transform = new UnityTransform(transform);
            _system = GetComponent<ParticleSystem>();
            _light = GetComponent<Light2D>();
        }

        private void LateUpdate()
        {
            if (_stickedTo is not { IsValid: true }) return;
            Transform.WorldPosition = _stickedTo.WorldPosition;
        }

        public void StickTo(ITransform transform)
        {
            _stickedTo = transform;
        }

        [Button]
        public void Play()
        {
            _system.Stop();
            _system.Play();

            _animation.TryCancel();
            _animation = LMotion.Create(0f, 1f, _system.main.duration)
                .WithEase(_curve)
                .WithOnComplete(() => _light.intensity = 0)
                .Bind(x => _light.intensity = x * _lightIntensity);
        }
    }
}
