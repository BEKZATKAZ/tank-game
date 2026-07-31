using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.StartCountdown;

namespace Tanks.UI.Tests.Gameplay
{
    public class StartCountdownTests : ViewModelTestBase<StartCountdownViewModel>
    {
        protected override StartCountdownViewModel CreateViewModel()
        {
            return new StartCountdownViewModel(Substitute.For<IMessageBus>());
        }

        [TestCase(0.3f)]
        [TestCase(0.6f)]
        [TestCase(1.5f)]
        public async Task Start_RunsCountdown(float duration)
        {
            ViewModel.Handle(new GameStarted { StartDelaySeconds = duration }, null);

            int intervalMillis = (int)(1000 * (duration / 3));
            await UniTask.Yield();

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(ViewModel.CountdownText[i], ViewModel.Title.Value);
                await UniTask.Delay(intervalMillis);
                await UniTask.Yield();
            }

            Assert.IsEmpty(ViewModel.Title.Value, "Should hide after the countdown");
        }
    }
}
