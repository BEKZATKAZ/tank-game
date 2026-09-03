using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tanks.Client.Items.Pickups
{
    public static class PickupVisualSetup
    {
        public static void Setup(out SpriteRenderer icon, out Light2D glow)
        {
            GameObject view = new GameObject("view", typeof(SpriteRenderer));
            GameObject glowObject = new GameObject("glow", typeof(Light2D));

            glowObject.transform.localScale = new Vector3(5, 5, 1);

            icon = view.GetComponent<SpriteRenderer>();
            icon.sortingOrder = 5;

            glow = glowObject.GetComponent<Light2D>();
            glow.intensity = 1;
            glow.pointLightOuterRadius = 8;
            glow.pointLightInnerRadius = 1;

            glow.shadowsEnabled = false;
            glow.volumetricEnabled = false;

            var lightType = typeof(Light2D);
            var quality = Light2D.NormalMapQuality.Fast;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;

            // ! Using reflection as these fields are not exposed
            lightType.GetField("m_NormalMapQuality", flags).SetValue(glow, quality);
            lightType.GetField("m_NormalMapDistance", flags).SetValue(glow, 8);
        }
    }
}
