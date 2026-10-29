using System.Collections.Generic;
using Tanks.Shared;
using Tanks.Shared.Entities;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tanks.Client.Entities
{
    public interface IEntityRenderer : INetworkComponent<IEntityBehaviour>
    {
        IEnumerable<SpriteRenderer> GetAllSpriteRenderers();
        IEnumerable<Light2D> GetAllLights();
    }
}
