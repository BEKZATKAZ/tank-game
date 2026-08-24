using System;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Rendering
{
    public interface ICamera
    {
        ITransform Transform { get; }
        float OrthographicSize { get; set; }
        float Aspect { get; }

        float2 ScreenToWorldPoint(float2 pointerPosition);
    }

    public sealed class UnityCamera : ICamera
    {
        public UnityCamera(Camera camera)
        {
            OriginalCamera = camera;
            Transform = new UnityTransform(camera.transform);
        }

        public Camera OriginalCamera { get; }
        public ITransform Transform { get; }

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
    }
}
