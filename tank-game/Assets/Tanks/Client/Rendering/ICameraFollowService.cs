using Tanks.Shared.Adapters;
using UnityEngine;

namespace Tanks.Client.Rendering
{
    public interface ICameraFollowService
    {
        void SetBounds(Bounds mapBounds);
        void SetFollowTarget(ITransform target);
    }
}
