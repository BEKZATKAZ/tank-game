using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Client.Entities;
using Tanks.Client.Input;
using Tanks.Client.Input.Bindings;
using Tanks.Core.Repositories;
using Tanks.Core.Settings;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Tanks.Client.Tests.Input
{
    public sealed class MovementInputTests : InputTestFixture
    {
        private ManualMovementInputService _input;
        private IClientEntityService _entities;
        private IEntityBehaviour _entity;
        private Action<int> _modeChanged;
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private ControlBindingService _bindings;

        public override void Setup()
        {
            base.Setup();
            _entities = Substitute.For<IClientEntityService>();
            _entity = Substitute.For<IEntityBehaviour>();
            ISettingsService settings = Substitute.For<ISettingsService>();
            _bindings = new ControlBindingService(Substitute.For<IFileSystem>());
            _bindings.BeginEdit();
            _bindings.SetPath("modern.reverse.keyboard.0", "<Keyboard>/leftShift");
            _bindings.SetPath("modern.up.keyboard.0", "<Keyboard>/w");
            _bindings.SetPath("classic.forward.keyboard.0", "<Keyboard>/w");
            _bindings.SetPath("modern.right.gamepad.0", "<Gamepad>/leftStick/right");
            _bindings.Apply();

            _entity.IsValid.Returns(true);
            _entity.IsLocalPlayer.Returns(true);
            _entity.GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(100, 100));
            _entities.GetPossessedEntity().Returns(_entity);
            settings.When(x => x.Bind(
                PlayerSetting.ControlsMode, Arg.Any<Action<int>>()))
                .Do(x => _modeChanged = x.Arg<Action<int>>());

            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _input = new ManualMovementInputService(settings, _entities, _bindings);
        }

        public override void TearDown()
        {
            _input.Dispose();
            _bindings.Dispose();
            base.TearDown();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReverseIsHeldAndReleaseRestoresCurrentMovement(bool gamepad)
        {
            _bindings.SetPath("modern.reverse.gamepad.0", "<Gamepad>/rightShoulder");
            _bindings.Apply();
            Press(_keyboard.wKey);
            Press(_keyboard.spaceKey);
            if (gamepad) Press(_gamepad.rightShoulder);
            else Press(_keyboard.leftShiftKey);

            Assert.That(_input.Read().Reverse, Is.True);
            Assert.That(_input.Read().Direction, Is.EqualTo(float2.zero));
            Assert.That(_input.Read().TurnInPlace, Is.False);

            InputSystem.Update();

            Assert.That(_input.Read().Reverse, Is.True);

            if (gamepad) Release(_gamepad.rightShoulder);
            else Release(_keyboard.leftShiftKey);

            Assert.That(_input.Read().Reverse, Is.False);
            Assert.That(_input.Read().Direction, Is.EqualTo(new float2(0, 1)));
            Assert.That(_input.Read().TurnInPlace, Is.True);
        }

        [TestCase("disabled")]
        [TestCase("mode")]
        [TestCase("respawn")]
        [TestCase("suspended")]
        [TestCase("ownership")]
        [TestCase("dead")]
        public void HeldReverseResumesAsSoonAsDrivingIsAllowed(string reason)
        {
            Press(_keyboard.leftShiftKey);

            switch (reason)
            {
                case "disabled":
                    _input.Disable();
                    Assert.That(_input.Read().Reverse, Is.False);
                    _input.Enable();
                    break;
                case "mode":
                    _modeChanged((int)ControlsMode.Classic);
                    Assert.That(_input.Read().Reverse, Is.False);
                    _modeChanged((int)ControlsMode.Modern);
                    break;
                case "respawn":
                    _entities.Changed += Raise.Event<PossessedEntityChanged>(
                        _entity, _entity);
                    break;
                case "suspended":
                    _bindings.Capture("shoot.keyboard.1");
                    Assert.That(_input.Read().Reverse, Is.False);
                    _bindings.CancelCapture();
                    break;
                case "ownership":
                    _entity.IsLocalPlayer.Returns(false);
                    Assert.That(_input.Read().Reverse, Is.False);
                    _entity.IsLocalPlayer.Returns(true);
                    break;
                case "dead":
                    _entity.GetBehaviour<IHealthBehaviour>().ServerState.Value
                        .Returns(new HealthState(0, 100));
                    Assert.That(_input.Read().Reverse, Is.False);
                    _entity.GetBehaviour<IHealthBehaviour>().ServerState.Value
                        .Returns(new HealthState(100, 100));
                    break;
            }

            InputSystem.Update();

            Assert.That(_input.Read().Reverse, Is.True);
        }

        [Test]
        public void StickNoiseIsNeutralAndPartialDeflectionIsPreserved()
        {
            Set(_gamepad.leftStick,
                Vector2.one * InputSystem.settings.defaultDeadzoneMin / 4);

            Assert.That(_input.Read().Direction, Is.EqualTo(float2.zero));

            float value = math.lerp(InputSystem.settings.defaultDeadzoneMin,
                InputSystem.settings.defaultDeadzoneMax, 0.5f);

            Set(_gamepad.leftStick, Vector2.right * value);

            Assert.That(_input.Read().Direction.x, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void UnknownModeUsesSimplifiedAndClassicMapsAreExclusive()
        {
            _modeChanged(int.MaxValue);
            Press(_keyboard.wKey);

            Assert.That(_input.Read().IsModern, Is.True);

            _modeChanged((int)ControlsMode.Classic);
            InputSystem.Update();

            Assert.That(_input.Read().IsModern, Is.False);
            Assert.That(_input.Read().Throttle, Is.EqualTo(1));
        }

        [Test]
        public void PressAndReleaseInOneInputUpdateDoesNotLatchReverse()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.LeftShift));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            InputSystem.Update();

            Assert.That(_input.Read().Reverse, Is.False);
        }
    }
}
