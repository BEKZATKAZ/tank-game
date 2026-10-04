using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Rendering;
using Tanks.Client.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Tests.Runtime.Weapons
{
    public class ShakeModuleTests
    {
        private ShakeModule _component;
        private ICameraShakeService _service;

        [SetUp]
        public void Setup()
        {
            _service = Substitute.For<ICameraShakeService>();
            _component = new GameObject().AddComponent<ShakeModule>();
            _component.Construct(_service);
        }

        [TearDown]
        public void TearDown()
        {
            if (_component) Object.DestroyImmediate(_component.gameObject);
        }

        [Test]
        public void OnDone_RunsHitstop()
        {
            _component.Transform.WorldPosition = new float2(6, 5);

            _component.Amplitude = 1;
            _component.DurationSeconds = 2;
            _component.Frequency = 3;
            _component.Radius = 4;

            _component.ProjectileDone();
            _service.Received(1).AddShake(Arg.Is<CameraImpulse>(x =>
                x.Amplitude == _component.Amplitude &&
                x.Duration == _component.DurationSeconds &&
                x.Frequency == _component.Frequency &&
                x.Radius == _component.Radius));

            // Should not run with zero amplitude
            _component.Amplitude = 0;
            _service.ClearReceivedCalls();

            _component.ProjectileDone();
            _service.DidNotReceiveWithAnyArgs().AddShake(default);
        }
    }
}
