using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Player;
using Tanks.Client.Rendering;
using Tanks.Shared.Driving;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Client.Tests.Player
{
    public class TurretInputServiceTests : InputTestFixture
    {
        private TurretInputService _input;
        private InputActionAsset _asset;

        public override void Setup()
        {
            base.Setup();

            _input = new TurretInputService();
            _asset = InputSystem.ListEnabledActions()[0].actionMap.asset;
        }

        public override void TearDown()
        {
            _input.Disable();

            // The generated Controls.Dispose uses Destroy, which is unavailable in EditMode.
            Object.DestroyImmediate(_asset);
            base.TearDown();
        }

        [Test]
        public void Read_MouseAndGamepadConnected_UsesStickForDirection()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();

            Set(mouse.position, new Vector2(1280, 720));
            Set(gamepad.rightStick, Vector2.up);

            TurretInput input = _input.Read();

            Assert.AreEqual(InputScheme.Gamepad, _input.CurrentScheme);
            Assert.AreEqual(new float2(0, 1), input.LookAtDirection);
            Assert.AreEqual(new float2(1280, 720), input.PointerPosition);
        }

        [Test]
        public void Read_MouseMovesAfterGamepadInput_SwitchesBackToMouse()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();

            Set(mouse.position, new Vector2(1280, 720));
            Set(gamepad.rightStick, Vector2.up);
            Set(mouse.position, new Vector2(640, 360));

            TurretInput input = _input.Read();

            Assert.AreEqual(InputScheme.KeyboardAndMouse, _input.CurrentScheme);
            Assert.AreEqual(new float2(640, 360), input.PointerPosition);
            Assert.AreEqual(new float2(0, 1), input.LookAtDirection);
        }

        [Test]
        public void Read_StickMovesAfterMouseInput_SwitchesBackToGamepad()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();

            Set(mouse.position, new Vector2(1280, 720));
            Set(gamepad.rightStick, Vector2.up);
            Set(mouse.position, new Vector2(640, 360));
            Assert.AreEqual(InputScheme.KeyboardAndMouse, _input.CurrentScheme);

            Set(gamepad.rightStick, Vector2.left);

            Assert.AreEqual(InputScheme.Gamepad, _input.CurrentScheme);
            Assert.AreEqual(new float2(-1, 0), _input.Read().LookAtDirection);
        }

        [Test]
        public void Read_UnrelatedActionPerformed_DoesNotChangeScheme()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            using var action = new InputAction(binding: "<Keyboard>/space");
            action.Enable();

            Set(gamepad.rightStick, Vector2.up);
            Press(keyboard.spaceKey);

            Assert.AreEqual(InputScheme.Gamepad, _input.CurrentScheme);
        }

        [Test]
        public void Update_MouseConnected_StickTurnsTurretAndReleaseKeepsDirection()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            var behaviour = Substitute.For<ITankTurretBehaviour>();
            behaviour.IsLocalPlayer.Returns(true);

            var component = new PlayerTurret(_input, Substitute.For<ICameraMainService>());
            component.Initialize(behaviour);

            Set(mouse.position, new Vector2(1280, 720));
            Set(gamepad.rightStick, Vector2.left);
            component.Update(0.01f);

            behaviour.Received(1).Tick(new float2(-1, 0), 0.01f);

            Set(gamepad.rightStick, Vector2.zero);
            behaviour.ClearReceivedCalls();

            component.Update(0.01f);

            behaviour.Received(1).Tick(new float2(-1, 0), 0.01f);
        }

        [Test]
        public void Read_Gamepad_PreservesProcessedStickMagnitude()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();

            Set(gamepad.rightStick, new Vector2(0.4f, 0.3f));

            float2 expected = gamepad.rightStick.ReadValue();
            float2 actual = _input.Read().LookAtDirection;

            Assert.AreEqual(InputScheme.Gamepad, _input.CurrentScheme);
            Assert.AreEqual(expected.x, actual.x, 0.0001f);
            Assert.AreEqual(expected.y, actual.y, 0.0001f);
            Assert.Less(math.length(actual), 1);
        }

        [Test]
        public void Update_GamepadNearDeadZoneAndReleased_KeepsLastDirection()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            var behaviour = Substitute.For<ITankTurretBehaviour>();
            behaviour.IsLocalPlayer.Returns(true);

            var component = new PlayerTurret(_input, Substitute.For<ICameraMainService>());
            component.Initialize(behaviour);

            Set(gamepad.rightStick, Vector2.up);
            component.Update(0.01f);
            behaviour.Received(1).Tick(new float2(0, 1), 0.01f);

            float minimum = InputSystem.settings.defaultDeadzoneMin;
            float maximum = InputSystem.settings.defaultDeadzoneMax;
            float nearDeadZone = math.lerp(minimum, maximum, 0.05f);
            Set(gamepad.rightStick, new Vector2(nearDeadZone, 0));
            behaviour.ClearReceivedCalls();

            component.Update(0.01f);

            behaviour.Received(1).Tick(new float2(0, 1), 0.01f);

            Set(gamepad.rightStick, Vector2.zero);
            behaviour.ClearReceivedCalls();

            component.Update(0.01f);

            behaviour.Received(1).Tick(new float2(0, 1), 0.01f);
        }
    }
}
