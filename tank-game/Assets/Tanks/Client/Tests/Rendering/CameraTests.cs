using NUnit.Framework;
using Tanks.Client.Rendering;
using Tanks.Shared.Adapters;
using UnityEngine;

namespace Tanks.Client.Tests.Rendering
{
    public class CameraTests
    {
        private UnityCamera _camera;

        [OneTimeSetUp]
        public void Setup()
        {
            Camera original = new GameObject("cam", typeof(Camera)).GetComponent<Camera>();
            _camera = new UnityCamera(original);
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            if (_camera.OriginalCamera == false) return;
            Object.DestroyImmediate(_camera.OriginalCamera.gameObject);
        }

        [Test]
        public void OrthographicSize_SetAndGet_ReturnsSameValue()
        {
            float expectedSize = 5;
            _camera.OrthographicSize = expectedSize;

            float actualSize = _camera.OrthographicSize;
            Assert.AreEqual(expectedSize, actualSize);

            float originalSize = _camera.OriginalCamera.orthographicSize;
            Assert.AreEqual(expectedSize, originalSize);
        }

        [Test]
        public void Transform_ReturnsAsOriginal()
        {
            UnityTransform transform = (UnityTransform)_camera.Transform;
            Assert.AreEqual(_camera.OriginalCamera.transform, transform.Transform);
        }
    }
}