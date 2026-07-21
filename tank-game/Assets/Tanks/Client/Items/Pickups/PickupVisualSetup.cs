using UnityEngine;

namespace Tanks.Client.Items.Pickups
{
    public sealed class PickupVisualSetup
    {
        static PickupVisualSetup()
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                anisoLevel = 0,
                filterMode = FilterMode.Point
            };

            texture.SetPixel(0, 0, Color.white);
            texture.Apply();

            _pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        }

        private static readonly Sprite _pixel;

        public void Setup(
            Transform parent, out SpriteRenderer iconRenderer, out SpriteRenderer glowRenderer)
        {
            GameObject view = new GameObject("view", typeof(SpriteRenderer));
            GameObject glow = new GameObject("glow", typeof(SpriteRenderer));

            view.transform.SetParent(parent, worldPositionStays: false);
            view.transform.localScale = new Vector3(2, 2, 1);

            glow.transform.SetParent(parent, worldPositionStays: false);
            glow.transform.localScale = new Vector3(5, 5, 1);

            iconRenderer = view.GetComponent<SpriteRenderer>();
            iconRenderer.sortingOrder = 5;

            glowRenderer = glow.GetComponent<SpriteRenderer>();
            glowRenderer.sortingOrder = 1;
            glowRenderer.sprite = _pixel;
        }
    }
}
