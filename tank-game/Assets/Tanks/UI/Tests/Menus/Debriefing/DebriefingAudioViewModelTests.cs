using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Audio;
using Tanks.Core;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;
using Tanks.UI.Menus.Debriefing;
using Tanks.UI.Menus.Debriefing.Services;
using Tanks.UI.Menus.Sections;
using Unity.Mathematics;

namespace Tanks.UI.Tests.Menus.Debriefing
{
    public sealed class DebriefingAudioViewModelTests
    {
        private IAudioInstance _sound;
        private IAudioPlayer _player;
        private PlayerLevelService _levels;
        private PlayerDebriefing _data;
        private DebriefingViewModel _model;
        private float _maxPitch;

        [SetUp]
        public void Setup()
        {
            IAudioFactory factory = Substitute.For<IAudioFactory>();
            ILocalPlayerService local = Substitute.For<ILocalPlayerService>();
            IMenuSectionService navigation = Substitute.For<IMenuSectionService>();

            _sound = Substitute.For<IAudioInstance>();
            _player = Substitute.For<IAudioPlayer>();

            PendingDebriefingService pending = new();
            _levels = new PlayerLevelService();
            _maxPitch = 2.25f;

            _data = new PlayerDebriefing
            {
                Rows = new StatDebriefing[5],
                BeforeLevel = new PlayerLevel { Value = 1 }
            };

            _data.Rows[0] = new StatDebriefing
            {
                Value = 4, XP = _levels.RequiredXP(_data.BeforeLevel.Value)
            };

            factory.CreateInstance("stat").Returns(_sound);
            local.LoadDataAsync().Returns(Task.FromResult(Result.Success()));
            pending.Store("match", new DebriefingPlayerResult { Value = _data });

            DebriefingAudioService audio = new(factory, _player);

            _model = new DebriefingViewModel(pending, navigation, local,
                new DebriefingAnimationService(_levels), audio);

            _model.ConfigureAudio("stat", "level", _maxPitch);
        }

        [TearDown]
        public void TearDown() => _model.Dispose();

        [Test]
        public void OpeningDelay_HidesRowsAndRemainsSilent()
        {
            _model.Initialize();
            _model.Tick(0.5f);

            Assert.That(_model.Animation.VisibleRows, Is.Zero);
            Assert.That(_model.Animation.Rows[0].XP, Is.Zero);
            _sound.DidNotReceive().Play();

            _model.Tick(0.5f);

            Assert.That(_model.Animation.VisibleRows, Is.EqualTo(1));
            Assert.That(_model.Animation.Rows[0].XP, Is.Zero);
            _sound.Received(1).Play();
        }

        [Test]
        public void Pitch_FollowsProgressAndResetsBeforeLevelUpSound()
        {
            List<float> levelPitches = new();

            float pitch = 0;

            _sound.When(value => value.SetPitch(Arg.Any<float>()))
                .Do(call => { pitch = call.Arg<float>(); });

            _player.When(value => value.PlayOneShot("level", default))
                .Do(call => { levelPitches.Add(pitch); });

            _model.Initialize();
            _model.Tick(1);
            _model.Tick(0.5f);

            Assert.That(pitch, Is.EqualTo(math.lerp(1, _maxPitch, 0.5f)));

            _model.Tick(0.5f);

            Assert.That(_model.Animation.Level.XP, Is.Zero);
            Assert.That(levelPitches, Is.EqualTo(new[] { 1f }));
            _sound.Received(1).Stop(false);
            _sound.Received(1).Dispose();
        }

        [Test]
        public void LargeTick_ResetsPitchForEveryLevelAndKeepsRemainder()
        {
            int first = _data.BeforeLevel.Value;

            int remainder = _levels.RequiredXP(first + 3) / 2;

            List<float> levelPitches = new();

            float pitch = 0;

            _data.Rows[0].XP = remainder;

            for (int level = first; level < first + 3; level++)
            {
                _data.Rows[0].XP += _levels.RequiredXP(level);
            }

            _sound.When(value => value.SetPitch(Arg.Any<float>()))
                .Do(call => { pitch = call.Arg<float>(); });

            _player.When(value => value.PlayOneShot("level", default))
                .Do(call => { levelPitches.Add(pitch); });

            _model.Initialize();
            _model.Tick(2);

            Assert.That(levelPitches, Is.EqualTo(new[] { 1f, 1f, 1f }));
            Assert.That(_model.Animation.Level.XP, Is.EqualTo(remainder));

            Assert.That(pitch, Is.EqualTo(math.lerp(1, _maxPitch,
                (float)remainder / _levels.RequiredXP(first + 3))));
        }

        [Test]
        public void ZeroRows_AppearInSequenceWithoutPlayingSound()
        {
            _data.Rows[0] = default;
            _model.Initialize();
            _model.Tick(1);

            Assert.That(_model.GetRow(0).IsVisible, Is.True);
            Assert.That(_model.GetRow(1).IsVisible, Is.False);

            _model.Tick(1.5f);

            Assert.That(_model.GetRow(1).IsVisible, Is.True);
            _sound.DidNotReceive().Play();
            _player.DidNotReceive().PlayOneShot(Arg.Any<string>(), default);
        }

        [Test]
        public void ZeroStatisticWithRewards_StillPlaysCountingSound()
        {
            _data.Rows[0].Value = 0;
            _model.Initialize();
            _model.Tick(1);
            _sound.Received(1).Play();
        }

        [TestCase(0.25f)]
        [TestCase(1.25f)]
        public void SkipAndDisposal_StopAudioAndPreventLaterCallbacks(float elapsed)
        {
            _model.Initialize();
            _model.Tick(elapsed);
            _model.Skip();
            _model.Dispose();

            if (elapsed > 1)
            {
                _sound.Received(1).Stop(false);
                _sound.Received(1).Dispose();
            }

            _sound.ClearReceivedCalls();
            _player.ClearReceivedCalls();
            _model.Tick(100);

            Assert.That(_sound.ReceivedCalls(), Is.Empty);
            Assert.That(_player.ReceivedCalls(), Is.Empty);
        }
    }
}
