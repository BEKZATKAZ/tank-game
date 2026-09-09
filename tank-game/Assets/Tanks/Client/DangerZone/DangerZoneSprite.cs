using Tanks.Client.Bootstrap;
using Tanks.Client.Visuals;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.DangerZone
{
    public sealed class DangerZoneSprite :
        NetworkComponent<IDangerZoneBehaviour>,
        INetworkMessageHandler<DangerZoneStarted>,
        IMessageHandler<GameStartedFromClient>
    {
        public DangerZoneSprite(IMessageBus bus, IStressService stress)
        {
            _bus = bus;
            _stress = stress;
        }

        public SpriteRenderer Renderer { get; private set; }
        public ITransform RendererTransform { get; private set; }

        private readonly IMessageBus _bus;
        private readonly IStressService _stress;

        private float _duration;
        private float2 _maxSize;
        private float2 _currentSize;

        private int _lastTick;
        private float _lastTickDelta;

        private float ShrinkStep(float deltaTime)
        {
            _currentSize -= deltaTime * _maxSize / _duration;
            _currentSize = math.max(_currentSize, 0);
            return math.cmax(_currentSize);
        }

        public void Handle(GameStartedFromClient message, IMessageSender sender)
        {
            Renderer.gameObject.SetActive(false);
            Behaviour.SetCurrentState(Rect.MinMaxRect(-10000, -10000, 10000, 10000));
        }

        public void Handle(DangerZoneStarted message)
        {
            _stress.SetMinStress(0.5f);

            _duration = message.ServerDuration;
            _maxSize = message.StartSize;
            _currentSize = _maxSize;

            // Done to keep up with the server state
            int tickDifference = _lastTick - message.ServerTick;
            if (tickDifference > 0) ShrinkStep(tickDifference * _lastTickDelta);

            RendererTransform.WorldPosition = message.StartSize * 0.5f;
            RendererTransform.LocalScale = message.StartSize;
            Renderer.gameObject.SetActive(true);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            _lastTick = tick;
            _lastTickDelta = tickDelta;
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Renderer.gameObject.activeInHierarchy == false) return;
            if (math.cmax(_currentSize) <= 0) return;

            float zoneSize = ShrinkStep(deltaTime);
            if (zoneSize <= 0) _stress.SetMinStress(0.95f);

            float2 difference = _maxSize - _currentSize;
            float2 min = difference * 0.5f;
            float2 max = _maxSize - difference * 0.5f;

            Renderer.sharedMaterial.SetFloat("_ZoneSize", zoneSize);
            Behaviour.SetCurrentState(Rect.MinMaxRect(min.x, min.y, max.x, max.y));
        }

        protected override void OnInitialize()
        {
            Renderer = new GameObject("danger-zone").AddComponent<SpriteRenderer>();
            Renderer.gameObject.SetActive(false);

            RendererTransform = new UnityTransform(Renderer.transform);
            RendererTransform.SetParent(Transform);

            // Sprite renderers render nothing without a sprite
            Texture2D pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            Sprite pixelSprite = Sprite.Create(pixel, new(0, 0, 1, 1), new(0.5f, 0.5f), 1);
            Renderer.sprite = pixelSprite;
            Renderer.sortingOrder = 15;
            Renderer.sharedMaterial = new Material(Shader.Find("Shader Graphs/DangerZone"));

            Behaviour.RegisterMessageHandler(this);
            _bus.RegisterHandler<GameStartedFromClient>(this);
        }

        protected override void OnDestroy()
        {
            if (Renderer) Object.Destroy(Renderer.gameObject);
            _bus.UnregisterHandler<GameStartedFromClient>(this);
        }
    }
}
