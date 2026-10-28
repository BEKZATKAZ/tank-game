using Unity.Mathematics;

namespace Tanks.Client.Rendering
{
    public readonly struct ViewportProjection
    {
        public ViewportProjection(
            float3 cameraPosition,
            quaternion cameraRotation,
            float2 worldSize,
            float unitsPerPixel,
            bool snapWorldPosition,
            float2 renderSize)
        {
            _cameraPosition = cameraPosition;
            if (unitsPerPixel > 0)
            {
                _cameraPosition.xy = math.round(cameraPosition.xy / unitsPerPixel) * unitsPerPixel;
            }

            _inverseRotation = math.inverse(cameraRotation);
            _worldSize = worldSize;
            _unitsPerPixel = snapWorldPosition ? unitsPerPixel : 0;
            _renderSize = renderSize;
        }

        private readonly float3 _cameraPosition;
        private readonly quaternion _inverseRotation;
        private readonly float2 _worldSize;
        private readonly float _unitsPerPixel;
        private readonly float2 _renderSize;

        public float2 WorldToViewportPoint(float2 worldPosition)
        {
            if (_unitsPerPixel > 0)
            {
                worldPosition = math.round(worldPosition / _unitsPerPixel) * _unitsPerPixel;
            }

            float3 local = math.rotate(_inverseRotation, new float3(worldPosition, 0) - _cameraPosition);
            float2 viewport = local.xy / _worldSize + 0.5f;

            if (math.all(_renderSize > 0))
            {
                viewport = math.round(viewport * _renderSize) / _renderSize;
            }

            return viewport;
        }
    }
}
