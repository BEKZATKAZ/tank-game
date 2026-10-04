using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Visuals
{
    public interface IVisualService
    {
        T Play<T>(VisualType type, float2 position = default, Angle rotation = default)
            where T : Component;
    }
}
