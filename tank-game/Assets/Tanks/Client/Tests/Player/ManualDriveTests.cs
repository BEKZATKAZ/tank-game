using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Player;
using Tanks.Shared.Driving;

namespace Tanks.Client.Tests.Player
{
    public class ManualDriveTests
    {
        private IInputService<ManualDriveInput> _input;
        private ManualPlayerDrive _component;

        [SetUp]
        public void Setup()
        {
            _input = Substitute.For<IInputService<ManualDriveInput>>();
            _component = new ManualPlayerDrive(_input);
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
    }
}
