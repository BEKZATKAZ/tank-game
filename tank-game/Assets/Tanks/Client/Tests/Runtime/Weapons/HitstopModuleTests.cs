using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Visuals.Hitstop;
using Tanks.Client.Weapons.Projectiles.Modules;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Tests.Runtime.Weapons
{
    public class HitstopModuleTests
    {
        private HitstopModule _component;
        private IHitstopService _service;

        [SetUp]
        public void Setup()
        {
            _service = Substitute.For<IHitstopService>();
            _component = new GameObject().AddComponent<HitstopModule>();
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

            _component.DurationSeconds = 0;
            _component.ProjectileDone();
            _service.DidNotReceiveWithAnyArgs().RunAsync(default, default);

            _component.DurationSeconds = 0.5f;
            _component.ProjectileDone();
            _service.Received(1).RunAsync(_component.Transform.WorldPosition, 0.5f);
        }
    }
}
