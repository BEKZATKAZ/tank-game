using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.UI.Gameplay.Effects;

namespace Tanks.UI.Tests.Gameplay
{
    public class EffectsTests : ViewModelTestBase<EffectsViewModel>
    {
        private IClientEntityService _clientEntityService;
        private IEntityBehaviour _player;
        private Dictionary<EffectType, EffectEntry> _effects;
        private Dictionary<EffectId, EffectEntry> _entries;

        protected override EffectsViewModel CreateViewModel()
        {
            _clientEntityService = Substitute.For<IClientEntityService>();
            _player = Substitute.For<IEntityBehaviour>();
            _player.IsValid.Returns(true);
            _clientEntityService.GetPossessedEntity().Returns(_player);

            _effects = new Dictionary<EffectType, EffectEntry>
            {
                [EffectType.Burning] = new EffectEntry(0, 5, null)
            };

            _player.GetBehaviour<IEffectOwnerBehaviour>().Effects.Returns(_effects);
            _entries = new Dictionary<EffectId, EffectEntry>();
            _player.GetBehaviour<IEffectOwnerBehaviour>().Entries.Returns(_entries);
            _player.GetBehaviour<IEffectOwnerBehaviour>().Entity.Returns(_player);

            return new EffectsViewModel(_clientEntityService);
        }

        [Test]
        public void Flash_UsesIndependentProgressAndPreservesOtherFullscreenEffects()
        {
            _effects[EffectType.Flashed] = new EffectEntry(100, 5, null);

            ViewModel.Tick(100, 0.1f);

            Assert.That(ViewModel.CurrentEffect.Value, Is.EqualTo(EffectType.Burning));
            Assert.That(ViewModel.FlashProgress.Value, Is.Zero);
            Assert.That(ViewModel.ActiveEffects.Value, Does.Contain(EffectType.Flashed));

            ViewModel.Tick(125, 0.1f);
            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(0.5f));

            ViewModel.Tick(150, 0.1f);
            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(1));
        }

        [Test]
        public void Flash_AloneDoesNotSelectLegacyFullscreenChannel()
        {
            _effects.Clear();
            _effects[EffectType.Flashed] = new EffectEntry(0, 5, null);

            ViewModel.Tick(10, 0.1f);

            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(0.2f).Within(0.001f));
        }

        [Test]
        public void Flash_RestartResetsProgressEvenWhenTypeDoesNotChange()
        {
            _effects[EffectType.Flashed] = new EffectEntry(0, 5, null);
            ViewModel.Tick(40, 0.1f);
            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(0.8f));

            _effects[EffectType.Flashed] = new EffectEntry(40, 2.5f, null);
            ViewModel.Tick(40, 0.1f);

            Assert.That(ViewModel.FlashProgress.Value, Is.Zero);
        }

        [Test]
        public void Flash_UsesStrongestEntryEvenWhenAnotherIdLastsLonger()
        {
            _effects[EffectType.Flashed] = new EffectEntry(0, 10, null);
            _entries[new EffectId(1)] = new EffectEntry(EffectType.Flashed, 0, 10, null);
            _entries[new EffectId(2)] = new EffectEntry(EffectType.Flashed, 75, 1, null);

            ViewModel.Tick(80, 0.1f);

            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(ViewModel.ActiveEffects.Value, Has.Exactly(1).EqualTo(EffectType.Flashed));
        }

        [Test]
        public void Flash_PossessionLostClearsImmediately()
        {
            _effects[EffectType.Flashed] = new EffectEntry(0, 5, null);
            ViewModel.Tick(10, 0.1f);

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, null);

            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(1));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void Flash_InvalidDurationDoesNotBlind(float duration)
        {
            _effects[EffectType.Flashed] = new EffectEntry(0, duration, null);

            ViewModel.Tick(10, 0.1f);

            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(1));
        }

        [Test]
        public void Flash_RemovalAndInvalidOwnerClearProgress()
        {
            _effects[EffectType.Flashed] = new EffectEntry(0, 5, null);
            ViewModel.Tick(10, 0.1f);

            _effects.Remove(EffectType.Flashed);
            ViewModel.Tick();
            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(1));

            _effects[EffectType.Flashed] = new EffectEntry(0, 5, null);
            ViewModel.Tick();
            _player.IsValid.Returns(false);
            ViewModel.Tick();

            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(1));
        }

        [Test]
        public void Flash_PossessionSwitchReadsExistingProgress()
        {
            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            next.IsValid.Returns(true);
            next.GetBehaviour<IEffectOwnerBehaviour>().Entity.Returns(next);
            next.GetBehaviour<IEffectOwnerBehaviour>().Effects.Returns(
                new Dictionary<EffectType, EffectEntry>
                {
                    [EffectType.Flashed] = new EffectEntry(100, 2, null)
                });
            ViewModel.Tick(110, 0.1f);

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, next);

            Assert.That(ViewModel.FlashProgress.Value, Is.EqualTo(0.5f));
        }

        [Test]
        public void Initialize_AlreadyPossessed_ReadsEffect()
        {
            ViewModel.Tick();
            Assert.That(ViewModel.CurrentEffect.Value, Is.EqualTo(EffectType.Burning));
            Assert.That(ViewModel.ActiveEffects.Value, Is.EqualTo(new[] { EffectType.Burning }));
        }

        [Test]
        public void Initialize_NoPossession_HasNoEffect()
        {
            _clientEntityService.GetPossessedEntity().Returns((IEntityBehaviour)null);
            using EffectsViewModel viewModel = new EffectsViewModel(_clientEntityService);

            Assert.That(viewModel.CurrentEffect.Value, Is.Null);
            Assert.That(viewModel.ActiveEffects.Value, Is.Null);
        }

        [Test]
        public void Initialize_InvalidPossession_HasNoEffect()
        {
            _player.IsValid.Returns(false);

            using EffectsViewModel viewModel = new EffectsViewModel(_clientEntityService);

            Assert.That(viewModel.CurrentEffect.Value, Is.Null);
            Assert.That(viewModel.ActiveEffects.Value, Is.Null);
        }

        [Test]
        public void Tick_ReplacedEffect_UpdatesCurrentEffect()
        {
            _effects.Clear();
            _effects[EffectType.Frozen] = new EffectEntry(0, 5, null);

            ViewModel.Tick();
            ViewModel.Tick();

            Assert.That(ViewModel.CurrentEffect.Value, Is.EqualTo(EffectType.Frozen));
            Assert.That(ViewModel.ActiveEffects.Value, Is.EqualTo(new[] { EffectType.Frozen }));
        }

        [Test]
        public void Tick_ExpiredEffect_ClearsCurrentEffect()
        {
            _effects.Clear();
            ViewModel.Tick();
            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
            Assert.That(ViewModel.ActiveEffects.Value, Is.Null);
        }

        [Test]
        public void PossessionChanged_ReadsNewEffect()
        {
            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            next.IsValid.Returns(true);

            next.GetBehaviour<IEffectOwnerBehaviour>().Entity.Returns(next);
            next.GetBehaviour<IEffectOwnerBehaviour>().Effects.Returns(
                new Dictionary<EffectType, EffectEntry> { [EffectType.Frozen] = default });

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, next);
            ViewModel.Tick();

            Assert.That(ViewModel.CurrentEffect.Value, Is.EqualTo(EffectType.Frozen));
            Assert.That(ViewModel.ActiveEffects.Value, Is.EqualTo(new[] { EffectType.Frozen }));
        }

        [Test]
        public void PossessionLost_ClearsEffectImmediately()
        {
            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, null);
            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
            Assert.That(ViewModel.ActiveEffects.Value, Is.Null);

            ViewModel.Tick();
            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
            Assert.That(ViewModel.ActiveEffects.Value, Is.Null);
        }

        [Test]
        public void Tick_InvalidPlayer_ClearsEffect()
        {
            _player.IsValid.Returns(false);

            ViewModel.Tick();

            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
            Assert.That(ViewModel.ActiveEffects.Value, Is.Null);
        }

        [Test]
        public void PossessionChanged_MissingEffectOwner_ClearsEffect()
        {
            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            next.IsValid.Returns(true);
            next.GetBehaviour<IEffectOwnerBehaviour>().Returns((IEffectOwnerBehaviour)null);

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, next);

            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
            Assert.That(ViewModel.ActiveEffects.Value, Is.Null);
        }

        [Test]
        public void Tick_SimultaneousEffects_PublishesDictionaryOrderAndPreservesCurrentEffect()
        {
            _effects.Clear();
            _effects[EffectType.Healing] = default;
            _effects[EffectType.Burning] = default;

            ViewModel.Tick();

            Assert.That(ViewModel.ActiveEffects.Value,
                Is.EqualTo(new[] { EffectType.Healing, EffectType.Burning }));
            Assert.That(ViewModel.CurrentEffect.Value, Is.EqualTo(EffectType.Healing));
        }

        [Test]
        public void Tick_OneEffectExpires_KeepsRemainingEffect()
        {
            _effects[EffectType.Healing] = default;
            ViewModel.Tick();

            _effects.Remove(EffectType.Burning);
            ViewModel.Tick();

            Assert.That(ViewModel.ActiveEffects.Value, Is.EqualTo(new[] { EffectType.Healing }));
            Assert.That(ViewModel.CurrentEffect.Value, Is.EqualTo(EffectType.Healing));
        }

        [Test]
        public void Tick_UnchangedEffects_DoesNotRepublish()
        {
            int updates = 0;
            ViewModel.ActiveEffects.Bind(_ => updates++);

            ViewModel.Tick();
            ViewModel.Tick();

            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void Tick_EffectDurationChanges_DoesNotRepublish()
        {
            int updates = 0;
            ViewModel.ActiveEffects.Bind(_ => updates++);

            _effects[EffectType.Burning] = new EffectEntry(0, 10, null);
            ViewModel.Tick();

            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void Tick_FinalTwoSeconds_StartsExpiringAtThreshold()
        {
            _effects[EffectType.Burning] = new EffectEntry(100, 5, null);

            ViewModel.Tick(129, 0.1f);
            Assert.That(ViewModel.ExpiringEffects.Value, Is.Null);

            ViewModel.Tick(130, 0.1f);
            Assert.That(ViewModel.ExpiringEffects.Value, Is.EqualTo(new[] { EffectType.Burning }));
        }

        [Test]
        public void Tick_ExtendedEffect_StopsExpiring()
        {
            ViewModel.Tick(30, 0.1f);
            Assert.That(ViewModel.ExpiringEffects.Value, Is.EqualTo(new[] { EffectType.Burning }));

            _effects[EffectType.Burning] = new EffectEntry(0, 10, null);
            ViewModel.Tick(30, 0.1f);

            Assert.That(ViewModel.ExpiringEffects.Value, Is.Null);
        }

        [Test]
        public void Tick_SimultaneousEffects_TracksExpirationSeparately()
        {
            _effects[EffectType.Healing] = new EffectEntry(0, 10, null);

            ViewModel.Tick(30, 0.1f);

            Assert.That(ViewModel.ExpiringEffects.Value, Is.EqualTo(new[] { EffectType.Burning }));
            Assert.That(ViewModel.ActiveEffects.Value,
                Is.EqualTo(new[] { EffectType.Burning, EffectType.Healing }));
        }

        [Test]
        public void Tick_RemovedEffect_ClearsExpiration()
        {
            ViewModel.Tick(30, 0.1f);
            _effects.Clear();

            ViewModel.Tick(31, 0.1f);

            Assert.That(ViewModel.ExpiringEffects.Value, Is.Null);
        }

        [Test]
        public void PossessionLost_ClearsExpirationImmediately()
        {
            ViewModel.Tick(30, 0.1f);

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, null);

            Assert.That(ViewModel.ExpiringEffects.Value, Is.Null);
        }

        [Test]
        public void Tick_UnchangedExpiration_DoesNotRepublish()
        {
            ViewModel.Tick(30, 0.1f);
            int updates = 0;
            ViewModel.ExpiringEffects.Bind(_ => updates++);

            ViewModel.Tick(31, 0.1f);
            ViewModel.Tick(32, 0.1f);

            Assert.That(updates, Is.EqualTo(1));
        }

        [Test]
        public void Tick_ChangedEffects_ReusesListAndNotifiesOnce()
        {
            IReadOnlyList<EffectType> effects = ViewModel.ActiveEffects.Value;
            int updates = 0;
            ViewModel.ActiveEffects.Bind(_ => updates++);

            _effects.Clear();
            _effects[EffectType.Frozen] = new EffectEntry(0, 5, null);
            ViewModel.Tick();
            ViewModel.Tick();

            Assert.That(ViewModel.ActiveEffects.Value, Is.SameAs(effects));
            Assert.That(effects, Is.EqualTo(new[] { EffectType.Frozen }));
            Assert.That(updates, Is.EqualTo(2));
        }

        [Test]
        public void Tick_ClearedEffects_PublishesNullOnceAndReusesListOnReturn()
        {
            IReadOnlyList<EffectType> effects = ViewModel.ActiveEffects.Value;
            int updates = 0;
            ViewModel.ActiveEffects.Bind(_ => updates++);

            _effects.Clear();
            ViewModel.Tick();
            ViewModel.Tick();

            Assert.That(ViewModel.ActiveEffects.Value, Is.Null);
            Assert.That(effects.Count, Is.Zero);
            Assert.That(updates, Is.EqualTo(2));

            _effects[EffectType.Healing] = new EffectEntry(0, 5, null);
            ViewModel.Tick();

            Assert.That(ViewModel.ActiveEffects.Value, Is.SameAs(effects));
            Assert.That(effects, Is.EqualTo(new[] { EffectType.Healing }));
            Assert.That(updates, Is.EqualTo(3));
        }

        [Test]
        public void Tick_ChangedExpiration_ReusesListAndNotifiesOnce()
        {
            _effects[EffectType.Healing] = new EffectEntry(0, 10, null);
            ViewModel.Tick(30, 0.1f);
            IReadOnlyList<EffectType> effects = ViewModel.ExpiringEffects.Value;
            int updates = 0;
            ViewModel.ExpiringEffects.Bind(_ => updates++);

            ViewModel.Tick(80, 0.1f);
            ViewModel.Tick(81, 0.1f);

            Assert.That(ViewModel.ExpiringEffects.Value, Is.SameAs(effects));
            Assert.That(effects, Is.EqualTo(new[] { EffectType.Burning, EffectType.Healing }));
            Assert.That(updates, Is.EqualTo(2));
        }

        [Test]
        public void Tick_ExtendedEffect_PublishesNullOnlyOnce()
        {
            ViewModel.Tick(30, 0.1f);
            int updates = 0;
            ViewModel.ExpiringEffects.Bind(_ => updates++);

            _effects[EffectType.Burning] = new EffectEntry(0, 10, null);
            ViewModel.Tick(30, 0.1f);
            ViewModel.Tick(31, 0.1f);

            Assert.That(ViewModel.ExpiringEffects.Value, Is.Null);
            Assert.That(updates, Is.EqualTo(2));
        }

        [Test]
        public void PossessionChanged_ReplacesAllActiveEffectsImmediately()
        {
            _effects[EffectType.Healing] = default;
            ViewModel.Tick();

            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            next.IsValid.Returns(true);
            next.GetBehaviour<IEffectOwnerBehaviour>().Entity.Returns(next);
            next.GetBehaviour<IEffectOwnerBehaviour>().Effects.Returns(
                new Dictionary<EffectType, EffectEntry> { [EffectType.Frozen] = default });

            _clientEntityService.Changed += Raise.Event<PossessedEntityChanged>(_player, next);

            Assert.That(ViewModel.ActiveEffects.Value, Is.EqualTo(new[] { EffectType.Frozen }));
        }
    }
}
