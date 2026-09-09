using NUnit.Framework;
using Tanks.Client.Rendering;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Tests.Rendering
{
    public class CameraTests
    {
        private UnityCamera _camera;

        [SetUp]
        public void Setup()
        {
            Camera original = new GameObject("cam", typeof(Camera)).GetComponent<Camera>();
            _camera = new UnityCamera(original);
        }

        [TearDown]
        public void TearDown()
        {
            if (_camera.OriginalCamera == false) return;
            Object.DestroyImmediate(_camera.OriginalCamera.gameObject);
        }

        [Test]
        public void Properties_SetAndGet_ReturnsSameValues()
        {
            float expectedSize = 5;
            float expectedAspect = 4f / 3;

            _camera.OrthographicSize = expectedSize;
            _camera.Aspect = expectedAspect;

            float actualSize = _camera.OrthographicSize;
            float actualAspect = _camera.Aspect;

            Assert.AreEqual(expectedSize, actualSize);
            Assert.AreEqual(expectedAspect, actualAspect);

            float originalSize = _camera.OriginalCamera.orthographicSize;
            float originalAspect = _camera.OriginalCamera.aspect;

            Assert.AreEqual(expectedSize, originalSize);
            Assert.AreEqual(expectedAspect, originalAspect);
        }

        [Test]
        public void Transform_ReturnsAsOriginal()
        {
            UnityTransform transform = (UnityTransform)_camera.BodyTransform;
            Assert.AreEqual(_camera.OriginalCamera.transform, transform.Transform);
        }

        [TestCase(100, 10)]
        [TestCase(1280, 778)]
        [TestCase(1920, 1080)]
        public void ScreenToWorldPoint_ComputesCorrectly(float x, float y)
        {
            Vector2 expected = _camera.OriginalCamera.ScreenToWorldPoint(new Vector3(x, y));
            Vector2 actual = _camera.ScreenToWorldPoint(new float2(x, y));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void StandardMethods()
        {
            Assert.AreEqual(
                _camera.OriginalCamera.GetHashCode(), _camera.GetHashCode(),
                "Invalid GetHashCode()");

            Assert.AreEqual(
                _camera.OriginalCamera.ToString(), _camera.ToString(),
                "Invalid ToString()");

            Assert.True(_camera.Equals(_camera), "Invalid Equals()");
        }
    }
}
