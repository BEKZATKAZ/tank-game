using NUnit.Framework;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Driving
{
    public class TankDriveTests
    {
        private ArcadeDriver _driver;
        private ArcadeDriverSettings _settings;

        [SetUp]
        public void Setup()
        {
            _settings = new ArcadeDriverSettings
            {
                AccelerationTime = 0.5f,
                MaxSpeed = 10,
                ReverseSpeed = 5,
                SteeringSpeed = Angle.FromDegrees(360)
            };

            _driver = new ArcadeDriver(_settings);
        }

        [TestCase(0)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(1)]
        public void Steer_ReturnsAngularVelocity(float steeringAmount)
        {
            float expected = _settings.SteeringSpeed.Degrees * steeringAmount;

            float right = _driver.Steer(steeringAmount).Degrees;
            Assert.AreEqual(-expected, right, 0.01f, "Right steering failed");

            float left = _driver.Steer(-steeringAmount).Degrees;
            Assert.AreEqual(expected, left, 0.01f, "Left steering failed");
        }

        [TestCase(2)]
        [TestCase(10)]
        [TestCase(100)]
        public void Drive_Acceleration_SimulatesCorrectly(int count)
        {
            float2 forward = new float2(0, 1);
            float deltaTime = _settings.AccelerationTime / count;

            for (int i = 0; i < count; i++)
            {
                float velocity = math.length(_driver.Drive(forward, 1, deltaTime));
                float acceleration = (i + 1f) / count;
                float expected = acceleration * _settings.MaxSpeed;
                
                Assert.AreEqual(expected, velocity, delta: 0.01f);
            }
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 1)]
        public void Drive_FullThrottle_ReturnsFullVelocity(
            float xForward, float yForward)
        {
            float2 forward = new float2(xForward, yForward);
            float deltaTime = _settings.AccelerationTime;

            // Full throttle
            float2 velocity = _driver.Drive(forward, 1, deltaTime);
            float2 expected = forward * _settings.MaxSpeed;
            Assert.AreEqual(expected, velocity, "Forgot to start the engine");

            // No throttle
            velocity = _driver.Drive(forward, 0, deltaTime);
            Assert.AreEqual(float2.zero, velocity, "Yes please, keep the throttle");
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(0, 1)]
        public void Drive_PartialThrottle_ReturnsPartialVelocity(
            float xForward, float yForward)
        {
            float2 forward = new float2(xForward, yForward);
            float deltaTime = _settings.AccelerationTime;

            for (float throttle = 0; throttle <= 1; throttle += 0.1f)
            {
                float2 velocity = _driver.Drive(forward, throttle, deltaTime);
                float2 expected = throttle * _settings.MaxSpeed * forward;
                Assert.AreEqual(expected, velocity, "We need full throttle don't we?");
            }

            // Reset
            _driver.Drive(forward, 0, _settings.AccelerationTime);

            for (float throttle = 0; throttle >= -1; throttle -= 0.1f)
            {
                float2 velocity = _driver.Drive(forward, throttle, deltaTime);
                float2 expected = throttle * _settings.ReverseSpeed * forward;
                Assert.AreEqual(expected, velocity, "Forgot to switch to reverse");
            }
        }

        [Test]
        public void SetMaxAcceleration_LimitsAcceleration()
        {
            float2 forward = new float2(1, 0);
            float time = _settings.AccelerationTime;

            float2 reverse = _driver.Drive(forward, -1, time);
            Assert.AreEqual(-_settings.ReverseSpeed * forward, reverse);
            _driver.SetMaxAcceleration(0);

            // Starts from 0 acceleration
            float2 result = _driver.Drive(new float2(1, 0), 1, time * 0.5f);
            Assert.AreEqual(_settings.MaxSpeed * 0.5f * forward, result);
        }
    }
}
