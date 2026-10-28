using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Tanks.Client.Rendering
{
    public interface ICamera
    {
        ITransform BodyTransform { get; }
        ITransform ShakeTransform { get; }

        float OrthographicSize { get; set; }
        float Aspect { get; }
        Rect ViewportRect { get; }

        float2 ScreenToWorldPoint(float2 pointerPosition);
        float2 WorldToViewportPoint(float2 worldPosition);
        float2 WorldToRenderedViewportPoint(float2 worldPosition);
    }

    public sealed class UnityCamera : ICamera
    {
        public UnityCamera(Camera camera)
        {
            OriginalCamera = camera;

            Transform cameraTransform = camera.transform;
            Transform body = cameraTransform.parent;

            BodyTransform = new UnityTransform(body ? body : cameraTransform);
            ShakeTransform = new UnityTransform(cameraTransform);
            _pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
        }

        public Camera OriginalCamera { get; }
        public Rect ViewportRect => OriginalCamera.rect;
        public ITransform BodyTransform { get; }
        public ITransform ShakeTransform { get; }

        private readonly PixelPerfectCamera _pixelPerfect;

        public float Aspect
        {
            get => OriginalCamera.aspect;
            set => OriginalCamera.aspect = value;
        }

        public float OrthographicSize
        {
            get => OriginalCamera.orthographicSize;
            set => OriginalCamera.orthographicSize = value;
        }

        public override int GetHashCode() => OriginalCamera.GetHashCode();
        public override string ToString() => OriginalCamera.ToString();

        public override bool Equals(object obj)
        {
            return obj is UnityCamera other && OriginalCamera.Equals(other.OriginalCamera);
        }

        public float2 ScreenToWorldPoint(float2 pointerPosition)
        {
            Vector3 result = OriginalCamera.ScreenToWorldPoint(new float3(pointerPosition.xy, 0));
            return ((float3)result).xy;
        }

        public float2 WorldToViewportPoint(float2 worldPosition)
        {
            Vector3 result = OriginalCamera.WorldToViewportPoint(new float3(worldPosition.xy, 0));
            return ((float3)result).xy;
        }

        public float2 WorldToRenderedViewportPoint(float2 worldPosition)
        {
            if (OriginalCamera.orthographic == false) return WorldToViewportPoint(worldPosition);

            Transform cameraTransform = OriginalCamera.transform;
            bool pixelPerfect = _pixelPerfect && _pixelPerfect.isActiveAndEnabled;
            float size = pixelPerfect ? _pixelPerfect.orthographicSize : OriginalCamera.orthographicSize;
            float2 worldSize = new float2(OriginalCamera.aspect, 1) * (size * 2);
            float spacing = 0;
            bool snapWorldPosition = false;
            float2 renderSize = float2.zero;

            if (pixelPerfect)
            {
                PixelPerfectCamera.GridSnapping snapping = _pixelPerfect.gridSnapping;
                bool upscale = snapping == PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
                snapWorldPosition = snapping == PixelPerfectCamera.GridSnapping.PixelSnapping;
                spacing = 1f / _pixelPerfect.assetsPPU;

                if (upscale) renderSize = math.round(worldSize * _pixelPerfect.assetsPPU);
                else if (snapWorldPosition == false) spacing /= math.max(1, _pixelPerfect.pixelRatio);
            }

            // Pixel Perfect Camera installs its snapped view matrix during rendering.
            // Rebuild the projection from this frame's pose instead of using that stale matrix.
            var projection = new ViewportProjection(
                cameraTransform.position, cameraTransform.rotation,
                worldSize, spacing, snapWorldPosition, renderSize);
            return projection.WorldToViewportPoint(worldPosition);
        }
    }
}
