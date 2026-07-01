using Tanks.Shared.Adapters;
using UnityEngine;

namespace Tanks.Client.Rendering
{
    public interface ICamera
    {
        ITransform Transform { get; }
        float OrthographicSize { get; set; }
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
    }
}