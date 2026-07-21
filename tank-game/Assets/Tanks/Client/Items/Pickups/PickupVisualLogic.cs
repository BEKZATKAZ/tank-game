using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupVisualLogic
    {
        public PickupVisualLogic(
            SpriteRenderer iconRenderer,
            SpriteRenderer glowRenderer)
        {
            IconRenderer = iconRenderer;
            GlowRenderer = glowRenderer;

            IconBlock = new MaterialPropertyBlock();
            GlowBlock = new MaterialPropertyBlock();

            IconRenderer.GetPropertyBlock(IconBlock);
            GlowRenderer.GetPropertyBlock(GlowBlock);
        }

        public SpriteRenderer IconRenderer { get; }
        public SpriteRenderer GlowRenderer { get; }

        public MaterialPropertyBlock IconBlock { get; }
        public MaterialPropertyBlock GlowBlock { get; }

        private float _elapsedSinceReset;

        public void Reset(IClientItemConfig config)
        {
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

        public void Tick(float deltaTime)
        {
            _elapsedSinceReset += deltaTime;

            float alpha = math.saturate(_elapsedSinceReset);

            IconBlock.SetFloat("_Brightness", math.lerp(10, 1, alpha));
            IconRenderer.SetPropertyBlock(IconBlock);

            GlowBlock.SetFloat("_Intensity", math.lerp(0.75f, 0.5f, alpha));
            GlowRenderer.SetPropertyBlock(GlowBlock);
        }
    }
}
