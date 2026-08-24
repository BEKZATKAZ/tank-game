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

        [SetUp]
        public void Setup()
        {
            _component = new GameObject("Velocity Tests", typeof(Rigidbody2D))
                .GetComponent<Rigidbody2D>();

            _adapter = new UnityVelocity(_component)
            {
                Linear = float2.zero,
                Angular = Angle.FromDegrees(0)
            };
        }

        [TearDown]
        public void TearDown()
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

        [Test]
        public void StandardMethods()
        {
            Assert.AreEqual(
               _component.GetHashCode(), _adapter.GetHashCode(),
                "Invalid GetHashCode()");

            Assert.AreEqual(
                _component.ToString(), _adapter.ToString(),
                "Invalid ToString()");

            Assert.True(_adapter.Equals(_adapter), "Invalid Equals()");
        }
    }
}
