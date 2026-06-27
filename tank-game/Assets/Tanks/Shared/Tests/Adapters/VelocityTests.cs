using NUnit.Framework;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Shared.Tests.Adapters
{
    public class VelocityTests
    {
        private IVelocity _adapter;
        private Rigidbody2D _component;

        [OneTimeSetUp]
        public void Setup()
        {
            _component = new GameObject("Velocity Tests", typeof(Rigidbody2D))
                .GetComponent<Rigidbody2D>();

            _adapter = new UnityVelocity(_component);
        }

        [SetUp]
        public void SetupEach()
        {
            _adapter.Linear = float2.zero;
            _adapter.Angular = Angle.FromDegrees(0);
        }

        [OneTimeTearDown]
        public void Dispose()
        {
            Object.DestroyImmediate(_component.gameObject);
        }

        [Test]
        public void Velocities_AdaptsCorrectly()
        {
            Assert.AreEqual((float2)_component.linearVelocity, _adapter.Linear);
            Assert.AreEqual(_component.angularVelocity, _adapter.Angular.Degrees);

            _adapter.Linear += new float2(10, 1);
            _adapter.Angular += Angle.FromRadians(math.PI);

            Assert.AreEqual(new float2(10, 1), (float2)_component.linearVelocity);
            Assert.AreEqual(180, _component.angularVelocity);
        }
    }
}