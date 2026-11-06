using NUnit.Framework;
using Tanks.Core.Settings;

namespace Tanks.Core.Tests.Settings
{
    public sealed class FrameRateLimitServiceTests
    {
        private sealed class Controller : IFrameRateController
        {
            public int TargetFrameRate { get; set; } = 120;
            public int VSyncCount { get; set; } = 1;
        }

        [Test]
        public void Limit_OverridesVSyncAndRestoresLatestPreferences()
        {
            Controller controller = new();
            FrameRateLimitService service = new(controller);
            object owner = new();

            service.SetLimit(owner, 20);

            Assert.That(controller.TargetFrameRate, Is.EqualTo(20));
            Assert.That(controller.VSyncCount, Is.Zero);

            service.SetFrameRate(240);
            service.SetVSync(2);
            service.SetLimit(owner, 15);

            Assert.That(controller.TargetFrameRate, Is.EqualTo(15));
            Assert.That(controller.VSyncCount, Is.Zero);

            service.RemoveLimit(owner);

            Assert.That(controller.TargetFrameRate, Is.EqualTo(240));
            Assert.That(controller.VSyncCount, Is.EqualTo(2));
        }

        [Test]
        public void MultipleLimits_KeepStrictestUntilItsOwnerLeaves()
        {
            Controller controller = new();
            FrameRateLimitService service = new(controller);
            object first = new();
            object second = new();

            service.SetLimit(first, 20);
            service.SetLimit(second, 15);
            service.RemoveLimit(first);

            Assert.That(controller.TargetFrameRate, Is.EqualTo(15));

            service.RemoveLimit(second);

            Assert.That(controller.TargetFrameRate, Is.EqualTo(120));
        }

        [Test]
        public void Limit_DoesNotRaiseAnAlreadyLowerFrameRate()
        {
            Controller controller = new();
            FrameRateLimitService service = new(controller);
            service.SetFrameRate(10);
            service.SetLimit(this, 20);

            Assert.That(controller.TargetFrameRate, Is.EqualTo(10));
        }
    }
}
