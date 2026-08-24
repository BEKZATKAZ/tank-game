using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Items.Pickups;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupView :
        NetworkComponent<IPickupBehaviour>,
        INetworkMessageHandler<PickupSpawned>
    {
        public PickupView(IClientItemDatabase database)
        {
            _database = database;
        }

        public SpriteRenderer IconRenderer { get; private set; }
        public SpriteRenderer GlowRenderer { get; private set; }

        public ITransform IconTransform { get; private set; }
        public ITransform GlowTransform { get; private set; }

        public MaterialPropertyBlock IconBlock { get; private set; }
        public MaterialPropertyBlock GlowBlock { get; private set; }

        private readonly IClientItemDatabase _database;
        private float _elapsedSinceReset;

        public void Handle(PickupSpawned message)
        {
            IClientItemConfig config = _database.Get(message.ItemId);

            if (IconRenderer.sharedMaterial != config.IconMaterial)
            {
                IconRenderer.sharedMaterial = config.IconMaterial;
            }

            if (GlowRenderer.sharedMaterial != config.GlowMaterial)
            {
                GlowRenderer.sharedMaterial = config.GlowMaterial;
            }

            IconBlock.SetFloat("_FrameCount", config.FrameCount);
            GlowBlock.SetColor("_Color", config.AccentColor);

            IconRenderer.SetPropertyBlock(IconBlock);
            GlowRenderer.SetPropertyBlock(GlowBlock);

            IconRenderer.sprite = config.Icon;
            _elapsedSinceReset = 0;
        }

        protected override void OnInitialize()
        {
            PickupVisualSetup.Setup(out var iconRenderer, out var glowRenderer);

            IconRenderer = iconRenderer;
            GlowRenderer = glowRenderer;

            IconTransform = new UnityTransform(IconRenderer.transform);
            GlowTransform = new UnityTransform(GlowRenderer.transform);

            IconBlock = new MaterialPropertyBlock();
            GlowBlock = new MaterialPropertyBlock();

            IconRenderer.GetPropertyBlock(IconBlock);
            GlowRenderer.GetPropertyBlock(GlowBlock);

            Behaviour.RegisterMessageHandler(this);
            OnDisable();
        }

        protected override void OnEnable()
        {
            IconRenderer.gameObject.SetActive(true);
            GlowRenderer.gameObject.SetActive(true);
        }

        protected override void OnDisable()
        {
            if (IconRenderer) IconRenderer.gameObject.SetActive(false);
            if (GlowRenderer) GlowRenderer.gameObject.SetActive(false);
        }

        protected override void OnUpdate(float deltaTime)
        {
            _elapsedSinceReset += deltaTime;
            float alpha = math.saturate(_elapsedSinceReset);

            IconBlock.SetFloat("_Brightness", math.lerp(10, 1, alpha));
            IconRenderer.SetPropertyBlock(IconBlock);

            GlowBlock.SetFloat("_Intensity", math.lerp(0.75f, 0.5f, alpha));
            GlowRenderer.SetPropertyBlock(GlowBlock);

            IconTransform.WorldPosition = Transform.WorldPosition;
            GlowTransform.WorldPosition = Transform.WorldPosition;
        }
    }
}
