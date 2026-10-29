using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Entities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tanks.Client.Entities
{
    public sealed class EntityRenderer : NetworkComponent<IEntityBehaviour>, IEntityRenderer
    {
        private Light2D[] _lights;
        private SpriteRenderer[] _sprites;

        public IEnumerable<Light2D> GetAllLights() => _lights;
        public IEnumerable<SpriteRenderer> GetAllSpriteRenderers() => _sprites;

        protected override void OnInitialize()
        {
            EntityBehaviour behaviour = Behaviour as EntityBehaviour;

            if (behaviour == false)
            {
                _sprites = new SpriteRenderer[0];
                _lights = new Light2D[0];
                return;
            }

            _sprites = behaviour.GetComponentsInChildren<SpriteRenderer>();
            _lights = behaviour.GetComponentsInChildren<Light2D>();
        }
    }
}
