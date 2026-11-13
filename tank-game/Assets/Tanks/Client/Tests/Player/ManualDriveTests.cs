using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Input.Driving;
using Tanks.Client.Player;
using Tanks.Shared.Driving;
using Tanks.Shared.Adapters;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Player
{
    public class ManualDriveTests
    {
        private IInputService<ManualDriveInput> _input;
        private ManualPlayerDrive _component;
        private DriveCommandService _commands;

        [SetUp]
        public void Setup()
        {
            _input = Substitute.For<IInputService<ManualDriveInput>>();
            _commands = new DriveCommandService();
            _component = new ManualPlayerDrive(
                _input, new DirectionalDriveResolverService(), _commands);
            _component.Initialize(Substitute.For<ITankDriveBehaviour>());
        }

        [TestCase(1, 0)]
        [TestCase(0.5f, 0)]
        [TestCase(0.5f, 1)]
        [TestCase(0.5f, -1)]
        [TestCase(-1, 0)]
        public void SendsDirectlyFromInput(float throttle, float steering)
        {
            _input.Read().Returns(new ManualDriveInput { Throttle = throttle, Steering = steering });
            _component.Update(0.02f);
            _component.Behaviour.Received(1).Tick(throttle, steering, 0.02f);
        }
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ModernCommandsUseDriverRateAndFeedAudio(bool handbrake, bool reverse)
        {
            ITransform transform = Substitute.For<ITransform>();
            ManualDriveInput input = new()
            {
                IsModern = true,
                Direction = new float2(0.5f, 0.5f),
                TurnInPlace = handbrake,
                Reverse = reverse
            };
            float rate = 70;
            float deltaTime = 0.2f;
            DirectionalDriveResolverService resolver = new();
            ManualDriveInput expected = resolver.Resolve(
                input.Direction, 20, handbrake, reverse, deltaTime, rate);

            transform.WorldRotation.Returns(Angle.FromDegrees(20));
            _component.Behaviour.Transform.Returns(transform);
            _component.Behaviour.TurnRate.Returns(rate);
            _input.Read().Returns(input);

            _component.Update(deltaTime);

            _component.Behaviour.Received().Tick(
                expected.Throttle, expected.Steering, deltaTime);
            Assert.That(_commands.Current.Throttle, Is.EqualTo(expected.Throttle));
        }

    }
}
