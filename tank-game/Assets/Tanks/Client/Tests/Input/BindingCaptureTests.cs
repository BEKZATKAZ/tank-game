using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Input;
using Tanks.Client.Input.Bindings;
using Tanks.Core.Repositories;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tanks.Client.Tests.Input
{
    public sealed class BindingCaptureTests : InputTestFixture
    {
        private ControlBindingService _service;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Gamepad _gamepad;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _service = new ControlBindingService(Substitute.For<IFileSystem>());
            _service.BeginEdit();
        }

        public override void TearDown()
        {
            _service.Dispose();
            base.TearDown();
        }

        [Test]
        public void CaptureWaitsForActivationReleaseAndFiltersDevicesAndMotion()
        {
            Press(_mouse.leftButton);
            _service.Capture("shoot.keyboard.1");
            InputSystem.Update();
            Assert.That(_service.IsCapturing, Is.True);
            Release(_mouse.leftButton);
            Press(_gamepad.buttonSouth);
            Set(_mouse.position, new Vector2(400, 300));
            Set(_mouse.scroll, Vector2.up);

            Assert.That(_service.IsCapturing, Is.True);
            Press(_keyboard.pKey);

            Assert.That(_service.IsCapturing, Is.False);
            Assert.That(_service.GetPath("shoot.keyboard.1"),
                Is.EqualTo("<Keyboard>/p"));
        }

        [Test]
        public void GamepadCaptureIgnoresNoiseAndAcceptsIndividualStickDirection()
        {
            _service.Capture("shoot.gamepad.1");
            InputSystem.Update();
            Press(_keyboard.pKey);
            Set(_gamepad.leftStick, Vector2.right * 0.01f);
            Assert.That(_service.IsCapturing, Is.True);
            Set(_gamepad.leftStick, Vector2.right);

            Assert.That(_service.GetPath("shoot.gamepad.1"),
                Is.EqualTo("<Gamepad>/leftStick/right"));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EscapeAndDisconnectCancelWithoutEdits(bool escape)
        {
            string original = _service.GetPath("shoot.keyboard.1");

            _service.Capture("shoot.keyboard.1");
            InputSystem.Update();
            if (escape) Press(_keyboard.escapeKey);
            else InputSystem.RemoveDevice(_gamepad);

            Assert.That(_service.IsCapturing, Is.False);
            Assert.That(_service.GetPath("shoot.keyboard.1"), Is.EqualTo(original));
            Assert.That(_service.IsDirty, Is.False);
        }

        [Test]
        public void CaptureRestoresEnabledInputAndLeavesDisabledInputDisabled()
        {
            TurretInputService enabled = new(_service);
            TurretInputService disabled = new(_service);

            disabled.Disable();
            _service.Capture("shoot.keyboard.1");
            Press(_mouse.leftButton);
            Assert.That(enabled.Read().Fire, Is.False);
            _service.CancelCapture();
            Release(_mouse.leftButton);
            Press(_mouse.leftButton);

            Assert.That(enabled.Read().Fire, Is.True);
            Assert.That(disabled.Read().Fire, Is.False);
            Assert.That(disabled.IsEnabled, Is.False);

            enabled.Dispose();
            disabled.Dispose();
        }

    }
}
