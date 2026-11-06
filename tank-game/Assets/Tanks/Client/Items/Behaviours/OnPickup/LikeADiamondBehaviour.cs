using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using UnityEngine.Rendering.Universal;

namespace Tanks.Client.Items.Behaviours.OnPickup
{
    [ItemBehaviour("like-a-diamond")]
    public sealed class LikeADiamondBehaviour : ItemBehaviourBase
    {
        private readonly Dictionary<IItemOwnerBehaviour, Dictionary<Light2D, float>> _lights = new();
        public override bool OncePerOwner => true;

        public override void OnPickup(IItemOwnerBehaviour owner)
        {
            if (_lights.ContainsKey(owner)) return;

            IEntityRenderer renderer = owner.Entity.Require<IEntityRenderer>();
            if (renderer == null) return;

            Dictionary<Light2D, float> lights = new();
            _lights.Add(owner, lights);

            foreach (Light2D light in renderer.GetAllLights())
            {
                if (light == false) continue;
                lights[light] = light.intensity;
                light.intensity = light.intensity * 15 + 5;
            }
        }

        public override void OnDestroy(IItemOwnerBehaviour owner)
        {
            if (_lights.Remove(owner, out var lights) == false) return;

            foreach ((Light2D light, float intensity) in lights)
            {
                if (light) light.intensity = intensity;
            }
        }
    }
}
