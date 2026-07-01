using NUnit.Framework;
using Tanks.Client.Rendering;
using UnityEngine;

namespace Tanks.Client.Tests.Rendering
{
    public class CameraMainTests
    {
        private ICameraMainService _service;
        private Camera _sceneMainCamera;
        private Camera _mainCamera;
        private Camera _secondaryCamera;

        [SetUp]
        public void Setup()
        {
            // Disable the actual main camera to avoid conflicts
            _sceneMainCamera = Camera.main;
            if (_sceneMainCamera) _sceneMainCamera.enabled = false;

            _service = new CameraMainService();
            _mainCamera = new GameObject("cam1", typeof(Camera)).GetComponent<Camera>();
            _secondaryCamera = new GameObject("cam2", typeof(Camera)).GetComponent<Camera>();

            _mainCamera.tag = "MainCamera";
            _secondaryCamera.tag = "MainCamera";
        }

        [TearDown]
        public void TearDown()
        {
            if (_sceneMainCamera) _sceneMainCamera.enabled = true;
            if (_mainCamera) Object.DestroyImmediate(_mainCamera.gameObject);
            if (_secondaryCamera) Object.DestroyImmediate(_secondaryCamera.gameObject);
        }

        [Test]
        public void Get_ReturnsMainCamera()
        {
            UnityCamera camera = (UnityCamera)_service.Get();
            Assert.AreEqual(_mainCamera, camera.OriginalCamera, "Does not see a camera");

            Object.DestroyImmediate(_mainCamera.gameObject);
            camera = (UnityCamera)_service.Get();
            Assert.AreEqual(_secondaryCamera, camera.OriginalCamera, "Main camera did not switch");
        }

        [Test]
        public void Get_NoCamera_ReturnsNull()
        {
            Object.DestroyImmediate(_mainCamera.gameObject);
            Object.DestroyImmediate(_secondaryCamera.gameObject);

            Assert.Null(_service.Get(), "Does it get the scene camera by any chance?");
        }
    }
}
