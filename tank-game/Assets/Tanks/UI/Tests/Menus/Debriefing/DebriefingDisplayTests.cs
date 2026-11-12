using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;
using Tanks.UI.Menus.Debriefing;
using Tanks.UI.Menus.Debriefing.Services;
using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Tests.Menus.Debriefing
{
    public sealed class DebriefingDisplayTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Rows_ExposeLocalizationKeysAndUnformattedValues(bool victory)
        {
            ILocalPlayerService player = Substitute.For<ILocalPlayerService>();
            IMenuSectionService navigation = Substitute.For<IMenuSectionService>();
            var audio = Substitute.For<IDebriefingAudioService>();

            PendingDebriefingService pending = new();
            PlayerLevelService levels = new();

            PlayerDebriefing data = new PlayerDebriefing
            {
                IsVictory = victory,
                Rows = new[]
                {
                    new StatDebriefing { Value = 7 },
                    new StatDebriefing { Value = 0 },
                    new StatDebriefing { Value = 2, PridePoints = -3 },
                    new StatDebriefing { Value = 4 },
                    new StatDebriefing()
                },
                BeforeLevel = new PlayerLevel { Value = 1 },
                AfterLevel = new PlayerLevel { Value = 1 }
            };

            player.LoadDataAsync().Returns(Task.FromResult(Result.Success()));
            pending.Store("match", new DebriefingPlayerResult { Value = data });

            using DebriefingViewModel model = new(pending, navigation, player,
                new DebriefingAnimationService(levels), audio);

            model.Initialize();
            model.Skip();

            string[] names = { "eliminations", "assists", "deaths", "score" };

            for (int row = 0; row < names.Length; row++)
            {
                DebriefingRowPresentation display = model.GetRow(row);

                Assert.That(display.LabelKey,
                    Is.EqualTo("debriefing." + names[row]));

                Assert.That(display.Value, Is.EqualTo(data.Rows[row]));
                Assert.That(display.IsVisible, Is.True);
            }

            string outcome = victory ? "debriefing.victory" : "debriefing.defeat";

            Assert.That(model.GetRow(4).LabelKey, Is.EqualTo(outcome));
            Assert.That(model.GetRow(2).NegativePridePoints, Is.True);
            Assert.That(model.GetRow(2).Value.PridePoints, Is.LessThan(0));
        }
    }
}
