using Tanks.Client.Bootstrap;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Tanks.Client.Visuals
{
    public sealed class VisualThemeChanger :
        MonoBehaviour,
        IMessageHandler<GameStartedFromClient>,
        IMessageHandler<TilemapGenerated>
    {
        [SerializeField] private Volume _volume;
        [SerializeField] private Light2D _globalLight;

        [SerializeField] private float _temperatureShiftChance = 0.5f;
        [SerializeField] private float _temperatureShift = 20;

        [SerializeField] private float _lightsOutChance = 0.3f;
        [SerializeField] private float _lightsOutIntensity = 0.01f;

        [Inject] private readonly IMessageBus _bus;

        private System.Random _random;
        private float _initialGlobalIntensity;

        private int _themePlayedTimes;
        private int _themeMaxTimes = 3;

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _random ??= new System.Random((int)message.Settings.Seed);
        }

        public void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            if (_themePlayedTimes++ < _themeMaxTimes) return;

            _themeMaxTimes = _random.Next(2, 4);
            _themePlayedTimes = 0;

            float random = (float)_random.NextDouble();
            SetLightsOut(random);
            SetTemperature(random);
        }

        private void SetLightsOut(float random)
        {
            float intensity = random < _lightsOutChance
                ? _lightsOutIntensity
                : _initialGlobalIntensity;

            _globalLight.intensity = intensity;
        }

        private void SetTemperature(float random)
        {
            if (_volume.profile.TryGet(out WhiteBalance component) == false) return;

            float value = random < _temperatureShiftChance ? 1 : 0;
            if (random < _temperatureShiftChance * 0.5f) value *= -1;

            component.temperature.overrideState = true;
            component.temperature.value = value * _temperatureShift;
        }

        private void Awake()
        {
            _initialGlobalIntensity = _globalLight.intensity;
        }

        private void Start()
        {
            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<GameStartedFromClient>(this);
        }

        private void OnDestroy()
        {
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _bus?.UnregisterHandler<GameStartedFromClient>(this);
        }
    }
}
