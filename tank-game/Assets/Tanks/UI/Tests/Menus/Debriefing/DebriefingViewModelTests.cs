using NSubstitute;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;
using Tanks.UI.Menus.Debriefing;
using Tanks.UI.Menus.Debriefing.Services;
using Tanks.UI.Menus.Sections;

namespace Tanks.UI.Tests.Menus.Debriefing
{
    public sealed class DebriefingViewModelTests
    {
        private PendingDebriefingService _pending;
        private IMenuSectionService _navigation;
        private ILocalPlayerService _player;
        private DebriefingViewModel _model;
        private PlayerDebriefing _data;

        [SetUp]
        public void Setup()
        {
            _navigation = Substitute.For<IMenuSectionService>();
            _player = Substitute.For<ILocalPlayerService>();

            var audio = Substitute.For<IDebriefingAudioService>();

            _pending = new PendingDebriefingService();

            _data = new PlayerDebriefing
            {
                MatchId = "match",
                Rows = new StatDebriefing[5],
                BeforeLevel = new PlayerLevel { Value = 1 }
            };

            PlayerLevelService levels = new();

            int required = levels.RequiredXP(_data.BeforeLevel.Value);

            int award = required + levels.RequiredXP(
                _data.BeforeLevel.Value + 1) + levels.RequiredXP(
                _data.BeforeLevel.Value + 2) / 2;

            _data.Rows[0] = new StatDebriefing
            {
                Value = 8, XP = award, SpareParts = 80
            };

            _data.Rows[2] = new StatDebriefing { Value = 3, PridePoints = -3 };

            _data.AfterLevel = levels.AddXP(_data.BeforeLevel,
                _data.Rows.Sum(row => row.XP));

            _player.LoadDataAsync().Returns(Task.FromResult(Result.Success()));

            _pending.Store("match", new DebriefingPlayerResult { Value = _data });

            _model = new DebriefingViewModel(_pending, _navigation, _player,
                new DebriefingAnimationService(levels), audio);
        }

        [TearDown]
        public void TearDown() => _model.Dispose();

        [Test]
        public void SharedTimeline_InterpolatesValuesThenWaitsBeforeNextRow()
        {
            _model.Initialize();
            _model.Tick(1);
            _model.Tick(0.5f);

            StatDebriefing row = _model.Animation.Rows[0];

            PlayerLevel expected = new PlayerLevelService().AddXP(_data.BeforeLevel,
                row.XP);

            Assert.That(row.Value, Is.EqualTo(_data.Rows[0].Value / 2));
            Assert.That(row.SpareParts, Is.EqualTo(_data.Rows[0].SpareParts / 2));
            Assert.That(_model.Animation.Level.Value, Is.EqualTo(expected.Value));
            Assert.That(_model.Animation.Level.XP, Is.EqualTo(expected.XP));
            Assert.That(_model.Animation.VisibleRows, Is.EqualTo(1));

            _model.Tick(0.5f);
            Assert.That(_model.Animation.IsCounting, Is.False);
            _model.Tick(0.5f);
            Assert.That(_model.Animation.VisibleRows, Is.EqualTo(2));
            Assert.That(_model.Animation.IsCounting, Is.True);
        }

        [Test]
        public void LargeTick_CrossesAllRowsAndLevels_WithoutFinalDelay()
        {
            int levelUps = 0;
            _model.Animation.LevelUp += () => levelUps++;
            _model.Initialize();
            _model.Tick(1 + _data.Rows.Length + (_data.Rows.Length - 1) * 0.5f);

            Assert.That(_model.Animation.Rows, Is.EqualTo(_data.Rows));
            Assert.That(_model.Animation.IsComplete, Is.True);
            Assert.That(_model.Animation.IsCounting, Is.False);
            Assert.That(_model.Animation.Level, Is.EqualTo(_data.AfterLevel));

            Assert.That(levelUps, Is.EqualTo(
                _data.AfterLevel.Value - _data.BeforeLevel.Value));
        }

        [TestCase(0.25f)]
        [TestCase(1.25f)]
        public void Skip_CompletesWithoutClosingOrPlayingSounds(float elapsed)
        {
            _model.Initialize();
            _model.Tick(elapsed);

            int levelUps = 0;
            _model.Animation.LevelUp += () => levelUps++;
            _model.Skip();
            _model.Tick(100);

            Assert.That(_model.Animation.Rows, Is.EqualTo(_data.Rows));
            Assert.That(_model.Animation.Level, Is.EqualTo(_data.AfterLevel));
            Assert.That(_model.Animation.VisibleRows, Is.EqualTo(_data.Rows.Length));
            Assert.That(_pending.HasPending, Is.True);
            Assert.That(levelUps, Is.Zero);

            _navigation.DidNotReceive().OpenSection(Arg.Any<MenuSectionType>(),
                Arg.Any<bool>());
        }

        [Test]
        public void Close_CancelsConsumesAndNavigates_WithoutLaterCallbacks()
        {
            _model.Initialize();
            _model.Close();
            _model.Tick(100);
            _pending.Store("match", new DebriefingPlayerResult { Value = _data });

            Assert.That(_pending.HasPending, Is.False);
            Assert.That(_model.Animation.IsCounting, Is.False);
            Assert.That(_model.Animation.Rows[0].Value, Is.Zero);

            _navigation.Received(1).OpenSection(MenuSectionType.Main);
        }

        [Test]
        public void MissingDelivery_ShowsFailureWithoutAnimation()
        {
            _pending.Consume();
            _pending.Expect("other-match");
            _model.Initialize();
            _model.Skip();
            _model.Tick(100);
            Assert.That(_model.IsFailure, Is.True);
            Assert.That(_model.Animation.IsCounting, Is.False);
        }

        [Test]
        public void RewardVisibility_UsesAppliedRewardsAndKeepsZeroStatisticRows()
        {
            _model.Initialize();
            _model.Skip();

            DebriefingRowPresentation elimination = _model.GetRow(0);
            DebriefingRowPresentation zero = _model.GetRow(1);
            DebriefingRowPresentation death = _model.GetRow(2);

            Assert.That(elimination.HasSpareParts, Is.True);
            Assert.That(elimination.HasXP, Is.True);
            Assert.That(elimination.HasPridePoints, Is.False);
            Assert.That(zero.IsVisible, Is.True);

            Assert.That(zero.HasSpareParts || zero.HasPridePoints || zero.HasXP,
                Is.False);

            Assert.That(death.HasPridePoints, Is.True);
            Assert.That(death.HasXP || death.HasSpareParts, Is.False);
        }
    }
}
