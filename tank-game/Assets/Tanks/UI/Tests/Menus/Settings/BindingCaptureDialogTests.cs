using NSubstitute;
using NUnit.Framework;
using System;
using Tanks.Client.Input.Bindings;
using Tanks.UI.Menus.Settings;

namespace Tanks.UI.Tests.Menus.Settings
{
    public sealed class BindingCaptureDialogTests
    {
        private IControlBindingService _bindings;
        private BindingCaptureDialogViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _bindings = Substitute.For<IControlBindingService>();

            _bindings.When(x => x.Capture(Arg.Any<string>())).Do(call =>
            {
                _bindings.CaptureSlot.Returns(call.Arg<string>());
                _bindings.IsCapturing.Returns(true);
                _bindings.Changed += Raise.Event<Action>();
            });

            _bindings.When(x => x.CancelCapture()).Do(_ => FinishCapture());
            _viewModel = new BindingCaptureDialogViewModel(_bindings);

            _viewModel.Initialize();
        }

        [TearDown]
        public void TearDown() => _viewModel.Dispose();

        [TestCase("shoot.keyboard.1", false)]
        [TestCase("shoot.gamepad.0", true)]
        public void CaptureOpensDevicePrompt(string slot, bool gamepad)
        {
            _viewModel.Capture(slot);

            Assert.That(_viewModel.IsVisible.Value, Is.True);
            Assert.That(_viewModel.IsGamepad, Is.EqualTo(gamepad));
            _bindings.Received(1).Capture(slot);
        }

        [Test]
        public void CompletionClosesDialogWithoutChangingTheCapturedBinding()
        {
            _viewModel.Capture("shoot.keyboard.1");

            FinishCapture();

            Assert.That(_viewModel.IsVisible.Value, Is.False);
            _bindings.DidNotReceiveWithAnyArgs().SetPath(default, default);
        }

        [Test]
        public void CancelPreservesBindings()
        {
            _viewModel.Capture("shoot.keyboard.1");

            _viewModel.Cancel();

            Assert.That(_viewModel.IsVisible.Value, Is.False);
            _bindings.DidNotReceiveWithAnyArgs().SetPath(default, default);
        }

        [Test]
        public void DisposeCancelsAndUnsubscribesAndCanReopen()
        {
            _viewModel.Capture("shoot.keyboard.1");

            _viewModel.Dispose();
            _bindings.IsCapturing.Returns(true);
            _bindings.Changed += Raise.Event<Action>();

            Assert.That(_viewModel.IsVisible.Value, Is.False);

            _viewModel.Initialize();

            Assert.That(_viewModel.IsVisible.Value, Is.True);
        }

        private void FinishCapture()
        {
            _bindings.IsCapturing.Returns(false);
            _bindings.CaptureSlot.Returns((string)null);
            _bindings.Changed += Raise.Event<Action>();
        }
    }
}
