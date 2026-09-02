using NSubstitute;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Entities;
using Tanks.Client.Input;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Tanks.Shared.Entities;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Player
{
    public class EngineSoundTests
    {
        private IInputService<ManualDriveInput> _input;
        private IAudioFactory _audioFactory;
        private ICameraMainService _cameraService;
        private IAudioInstance _mockAudioInstance;
        private IClientEntityService _clientEntityService;
        private EngineSound _component;

        [SetUp]
        public void Setup()
        {
            _input = Substitute.For<IInputService<ManualDriveInput>>();
            _audioFactory = Substitute.For<IAudioFactory>();
            _cameraService = Substitute.For<ICameraMainService>();
            _clientEntityService = Substitute.For<IClientEntityService>();
            _mockAudioInstance = Substitute.For<IAudioInstance>();

            _component = new EngineSound(
                _input, _audioFactory, _cameraService, _clientEntityService);

            _audioFactory
                .CreateInstance(Arg.Any<string>())
                .Returns(_mockAudioInstance);

            _component.Initialize(Substitute.For<ITankDriveBehaviour>());
            _component.Behaviour.Transform.Returns(Substitute.For<ITransform>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [TestCase(0)]
        [TestCase(0.4f)]
        [TestCase(1)]
        [TestCase(-0.9f)]
        [TestCase(-1)]
        public void Throttle_LocalPlayer_Accelerates(float throttleInput)
        {
            _component.Behaviour.IsLocalPlayer.Returns(true);
            _input.Read().Returns(new ManualDriveInput { Throttle = throttleInput });
            _component.Update(1);

            _mockAudioInstance
                .Received(1)
                .SetParameter(Arg.Any<string>(), math.abs(throttleInput));

            // Reset
            _input.Read().Returns(new ManualDriveInput { Throttle = 0 });
            _mockAudioInstance.ClearReceivedCalls();

            _component.Update(1);
            _mockAudioInstance.Received(1).SetParameter(Arg.Any<string>(), 0);
        }

        [Test]
        public void Throttle_OtherPlayer_Accelerates()
        {
            _component.Behaviour.IsLocalPlayer.Returns(false);
            _component.Transform.WorldPosition.Returns(float2.zero);

            _component.Update(1000);
            _component.Update(1);
            _component.Update(0.1f);

            _mockAudioInstance.Received(3).SetParameter(Arg.Any<string>(), 0);
            _mockAudioInstance.ClearReceivedCalls();

            _component.Transform.WorldPosition.Returns(new float2(10, 1));
            _component.Update(1);

            _mockAudioInstance.Received(1).SetParameter(Arg.Any<string>(), 1);
            _component.Update(1);
            _mockAudioInstance.Received(1).SetParameter(Arg.Any<string>(), 0);
        }
    }
}
