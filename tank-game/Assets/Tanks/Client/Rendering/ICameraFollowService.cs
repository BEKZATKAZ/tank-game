using Tanks.Shared.Adapters;
using UnityEngine;

namespace Tanks.Client.Rendering
{
    public interface ICameraFollowService
    {
        void SetDefault(Bounds mapBounds, bool forceOverride);

        void SetFollowTarget(ITransform target, float orthographicSize);
    }
}
