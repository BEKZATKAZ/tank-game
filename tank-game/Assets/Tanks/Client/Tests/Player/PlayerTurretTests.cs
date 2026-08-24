using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Shared.Adapters;
using Tanks.Shared.Driving;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Player
{
    public class PlayerTurretTests
    {
        private IInputService<TurretInput> _input;
        private ICameraMainService _cameraService;
        private ICamera _mainCamera;
        private PlayerTurret _component;

        [SetUp]
        public void Setup()
        {
            _input = Substitute.For<IInputService<TurretInput>>();
            _cameraService = Substitute.For<ICameraMainService>();

            _component = new PlayerTurret(_input, _cameraService);
            _component.Initialize(Substitute.For<ITankTurretBehaviour>());
            _component.Transform.Returns(Substitute.For<ITransform>());

            _mainCamera = Substitute.For<ICamera>();
            _cameraService.Get().Returns(_mainCamera);
        }

        [TestCase(0.5f, 0)]
        [TestCase(1.5f, 20)]
        [TestCase(10, 1)]
        public void WithPointer_CalculatesDirectionFromCamera(float playerX, float playerY)
        {
            float2 pointerPosition = new float2(1280, 720);
            float2 worldPoint = new float2(5, 0);

            float2 playerPosition = new float2(playerX, playerY);
            float2 expectedDirection = math.normalizesafe(worldPoint - playerPosition);

            _component.Transform.WorldPosition.Returns(playerPosition);
            _mainCamera.ScreenToWorldPoint(pointerPosition).Returns(worldPoint);

            _input.CurrentScheme.Returns(InputScheme.KeyboardAndMouse);
            _input.Read().Returns(new TurretInput { PointerPosition = pointerPosition });

            _component.Update(0.01f);
            _component.Behaviour.Received(1).Tick(expectedDirection, 0.01f);
        }

        [TestCase(1, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 1)]
        [TestCase(0, -1)]
        public void WithJoystick_CalculatesDirectionFromInput(float x, float y)
        {
            float2 direction = math.normalizesafe(new float2(x, y));

            _input.CurrentScheme.Returns(InputScheme.Gamepad);
            _input.Read().Returns(new TurretInput { LookAtDirection = direction });

            _component.Update(0.01f);
            _component.Behaviour.Received(1).Tick(direction, 0.01f);

            _input.Read().Returns(new TurretInput { LookAtDirection = float2.zero });
            _component.Behaviour.ClearReceivedCalls();

            // When the joystick is untouched, keep the last direction
            _component.Update(0.01f);
            _component.Behaviour.Received(1).Tick(direction, 0.01f);
        }
    }
}
