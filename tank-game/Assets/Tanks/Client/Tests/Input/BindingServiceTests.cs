using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System;
using Tanks.Client.Input.Bindings;
using Tanks.Client.Input;
using Tanks.Core.Repositories;
using UnityEngine.InputSystem;

namespace Tanks.Client.Tests.Input
{
    public sealed class BindingServiceTests : InputTestFixture
    {
        private IFileSystem _files;
        private ControlBindingService _service;
        private Controls _controls;
        private string _saved;

        public override void Setup()
        {
            base.Setup();
            _saved = null;
            InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Gamepad>();
            _files = Substitute.For<IFileSystem>();
            _service = new ControlBindingService(_files);
            _controls = new Controls();
            _files.Exists(Arg.Any<string>()).Returns(x => _saved != null);
            _files.ReadAllText(Arg.Any<string>()).Returns(x => _saved);
            _files.When(x => x.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
                .Do(x => _saved = x.ArgAt<string>(1));

            _service.Register(_controls.asset);
            _service.BeginEdit();
        }

        public override void TearDown()
        {
            _service.Dispose();
            InputAssetLifetime.Destroy(_controls.asset);
            base.TearDown();
        }

        [TestCase("modern.up.keyboard.0", "<Keyboard>/i")]
        [TestCase("modern.up.keyboard.1", "<Mouse>/rightButton")]
        [TestCase("modern.left.gamepad.0", "<Gamepad>/rightStick/left")]
        [TestCase("modern.left.gamepad.1", "<Gamepad>/dpad/down")]
        [TestCase("shoot.gamepad.1", "<Gamepad>/buttonSouth")]
        public void PendingBindingsApplyAndReloadByStableId(string slot, string path)
        {
            string original = RuntimePath(slot);

            _service.SetPath(slot, path);

            Assert.That(_service.IsDirty, Is.True);
            Assert.That(RuntimePath(slot), Is.EqualTo(original));

            Assert.That(_service.Apply(), Is.True);
            ControlBindingService reloaded = new(_files);
            Controls newControls = new();

            reloaded.Register(newControls.asset);

            Assert.That(RuntimePath(slot), Is.EqualTo(path));
            Assert.That(reloaded.GetPath(slot), Is.EqualTo(path));
            Assert.That(_service.IsDirty, Is.False);

            reloaded.Dispose();
            InputAssetLifetime.Destroy(newControls.asset);
        }

        [TestCase("modern.up.keyboard.0")]
        [TestCase("modern.up.keyboard.1")]
        [TestCase("modern.up.gamepad.0")]
        [TestCase("modern.up.gamepad.1")]
        public void ExplicitClearingSurvivesRestartAndResetRestoresDefaults(
            string slot)
        {
            string original = _service.GetPath(slot);

            _service.SetPath(slot, "");
            _service.Apply();
            ControlBindingService reloaded = new(_files);

            Assert.That(reloaded.GetPath(slot), Is.Empty);
            reloaded.Dispose();
            _service.Reset();

            Assert.That(_service.GetPath(slot), Is.EqualTo(original));
            Assert.That(RuntimePath(slot), Is.EqualTo(original));
            Assert.That(_service.IsDirty, Is.False);
        }

        [Test]
        public void RevertDiscardsPendingAndFailureRetainsThemForRetry()
        {
            const string SLOT = "shoot.keyboard.1";
            string original = _service.GetPath(SLOT);

            _service.SetPath(SLOT, "<Keyboard>/p");
            _service.Revert();
            Assert.That(_service.GetPath(SLOT), Is.EqualTo(original));
            _service.SetPath(SLOT, "<Keyboard>/p");
            _files.When(x => x.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
                .Do(_ => throw new InvalidOperationException("disk unavailable"));

            Assert.That(_service.Apply(), Is.False);
            Assert.That(_service.IsDirty, Is.True);
            Assert.That(_service.GetPath(SLOT), Is.EqualTo("<Keyboard>/p"));
            Assert.That(RuntimePath(SLOT), Is.EqualTo(original));
            Assert.That(_service.Error, Is.Not.Empty);
        }

        [TestCase("{broken")]
        [TestCase("{\"Version\":999,\"Overrides\":{}}")]
        public void MalformedOrUnsupportedFileLoadsDefaults(string json)
        {
            string slot = ControlBindingSlots.BindingIds.Keys.First();
            string original = _service.GetPath(slot);
            _saved = json;
            ControlBindingService reloaded = new(_files);

            Assert.That(reloaded.GetPath(slot), Is.EqualTo(original));
            reloaded.Dispose();
        }

        [Test]
        public void InvalidEntriesDoNotDiscardValidOverrides()
        {
            _saved = "{\"Version\":1,\"Overrides\":{" +
                "\"shoot.keyboard.1\":\"<Keyboard>/p\"," +
                "\"unknown.keyboard.0\":\"<Keyboard>/q\"," +
                "\"shoot.gamepad.1\":45," +
                "\"modern.down.gamepad.0\":\"<Gamepad>/leftStick/up/extra\"," +
                "\"modern.up.keyboard.0\":\"<Mouse>/position\"}}";
            ControlBindingService reloaded = new(_files);

            Assert.That(reloaded.GetPath("shoot.keyboard.1"),
                Is.EqualTo("<Keyboard>/p"));
            Assert.That(reloaded.GetPath("modern.up.keyboard.0"),
                Is.EqualTo(_service.GetPath("modern.up.keyboard.0")));
            Assert.That(reloaded.GetPath("modern.down.gamepad.0"),
                Is.EqualTo(_service.GetPath("modern.down.gamepad.0")));

            reloaded.Dispose();
        }

        [Test]
        public void DuplicateCompositePartsDoNotDoubleTheirValue()
        {
            _service.SetPath("modern.up.keyboard.0", "<Keyboard>/i");
            _service.SetPath("modern.up.keyboard.1", "<Keyboard>/i");
            _service.Apply();
            InputAction direction = _controls.asset.FindAction(
                "ModernMovement/Direction");

            direction.Enable();
            Press(Keyboard.current.iKey);

            Assert.That(direction.ReadValue<UnityEngine.Vector2>().y, Is.EqualTo(1));
        }

        private string RuntimePath(string slot)
        {
            InputBinding mask = new()
            {
                id = Guid.Parse(ControlBindingSlots.BindingIds[slot])
            };
            int index = _controls.asset.FindBinding(mask, out InputAction action);

            return action.bindings[index].effectivePath;
        }
    }
}
