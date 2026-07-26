using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Shared;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.StartCountdown;

namespace Tanks.UI.Tests.Gameplay
{
    public class StartCountdownTests :
        PresenterTestBase<StartCountdownPresenter, StartCountdownModel>
    {
        protected override StartCountdownPresenter CreatePresenter()
        {
            return new StartCountdownPresenter(Substitute.For<IMessageBus>());
        }

        [TestCase(0.3f)]
        [TestCase(0.6f)]
        [TestCase(1.5f)]
        public async Task Start_RunsCountdown(float duration)
        {
            Presenter.Handle(new GameStarted { StartDelaySeconds = duration }, null);

            int intervalMillis = (int)(1000 * (duration / 3));
            await UniTask.Yield();

            for (int i = 0; i < 4; i++)
            {
                Assert.False(IsHidden, "Should be shown during the countdown");
                Assert.AreEqual(Presenter.CountdownText[i], LatestShownModel.Text);

                await UniTask.Delay(intervalMillis);
                await UniTask.Yield();
            }

            Assert.True(IsHidden, "Should hide after the countdown");
        }
    }
}
