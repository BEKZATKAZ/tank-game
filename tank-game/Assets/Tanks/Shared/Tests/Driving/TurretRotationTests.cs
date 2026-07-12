using NUnit.Framework;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Tanks.Shared.Tests.Adapters;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Driving
{
    public class TurretRotationTests
    {
        private TankTurretRotation _rotation;
        private MockTransform _transform;
        private Angle _turnSpeed;

        [SetUp]
        public void Setup()
        {
            _turnSpeed = Angle.FromDegrees(180);
            _transform = new MockTransform();
            _rotation = new TankTurretRotation(_transform, _turnSpeed);
        }

        [Test]
        public void TurnAt_ShouldNotGoPast()
        {
            float2 targetPosition = new float2(-5.5f, 0.1f);
            float currentAngle = _transform.WorldRotation.Degrees;

            for (int i = 0; i < 1; i++)
            {
                _rotation.TurnAt(targetPosition, 0.1f);

                Assert.AreNotEqual(currentAngle, _transform.WorldRotation.Degrees,
                    $"Stuck at iteration {i + 1}");

                currentAngle = _transform.WorldRotation.Degrees;
            }

            _rotation.TurnAt(targetPosition, 1);
            currentAngle = _transform.WorldRotation.Degrees;

            for (int i = 0; i < 10; i++)
            {
                _rotation.TurnAt(targetPosition, 1);

                Assert.AreEqual(currentAngle, _transform.WorldRotation.Degrees, 0.05f,
                    "Turning past the target");
            }
        }
    }
}
