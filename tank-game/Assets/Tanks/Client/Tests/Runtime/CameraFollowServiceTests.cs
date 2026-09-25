using NSubstitute;
using NUnit.Framework;
using System.Collections;
using Tanks.Client.Rendering;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tanks.Client.Tests.Runtime
{
    public class CameraFollowServiceTests
    {
        private GameObject _gameObject;
        private CameraFollowService _service;
        private ICameraMainService _cameraMain;
        private ICamera _camera;
        private ITransform _target;

        [SetUp]
        public void Setup()
        {
            _cameraMain = Substitute.For<ICameraMainService>();
            _camera = Substitute.For<ICamera>();
            _target = Substitute.For<ITransform>();

            _camera.Aspect.Returns(1);
            _cameraMain.Get().Returns(_camera);

            _gameObject = new GameObject("CameraFollowService");
            _service = _gameObject.AddComponent<CameraFollowService>();
            _service.Inject(_cameraMain);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameObject);
        }

        [UnityTest]
        public IEnumerator LateUpdate_WithoutTarget_FollowsDefault()
        {
            _camera.BodyTransform.WorldPosition.Returns(float2.zero);
            _camera.OrthographicSize.Returns(5);

            _service.SetBounds(new Bounds(new Vector3(10, 20, 0), new Vector3(20, 10, 0)));
            yield return null;

            _camera.BodyTransform.Received().WorldPosition = Arg.Any<float2>();
        }

        [UnityTest]
        public IEnumerator LateUpdate_WithInvalidTarget_FollowsDefault()
        {
            _target.IsValid.Returns(false);

            _service.SetBounds(new Bounds(Vector3.zero, new Vector3(20, 20, 0)));
            _service.SetFollowTarget(_target, true);

            yield return null;
            _camera.BodyTransform.Received().WorldPosition = Arg.Any<float2>();
        }

        [UnityTest]
        public IEnumerator LateUpdate_WithValidTarget_FollowsTarget()
        {
            _target.IsValid.Returns(true);
            _target.WorldPosition.Returns(new float2(4, 3));

            _camera.BodyTransform.WorldPosition.Returns(float2.zero);
            _camera.OrthographicSize.Returns(5);

            _service.SetBounds(new Bounds(Vector3.zero, new Vector3(20, 20, 0)));
            _service.SetFollowTarget(_target, false);

            yield return null;

            _camera.BodyTransform.Received().WorldPosition =
                Arg.Is<float2>(x => x.x > 0 && x.x <= 4 && x.y > 0 && x.y <= 3);
        }
    }
}
