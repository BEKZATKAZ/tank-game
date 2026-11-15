using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using Tanks.Audio;
using Tanks.Client.Entities;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.UI.Gameplay.Effects;

namespace Tanks.UI.Tests.Gameplay
{
    public sealed class RetrofierEffectsTests : ViewModelTestBase<EffectsViewModel>
    {
        private readonly List<ItemIdentifier> _items = new();
        private IClientEntityService _entities;
        private IAudioPlayer _audio;
        private IEntityBehaviour _player;

        protected override EffectsViewModel CreateViewModel()
        {
            _entities = Substitute.For<IClientEntityService>();
            _audio = Substitute.For<IAudioPlayer>();
            _player = Substitute.For<IEntityBehaviour>();
            _items.Clear();

            _player.IsValid.Returns(true);
            _player.GetBehaviour<IItemOwnerBehaviour>().Items.Returns(_items);
            _entities.GetPossessedEntity().Returns(_player);

            return new EffectsViewModel(_entities, _audio);
        }

        [Test]
        public void Pickup_EnablesWithoutStatusEffectsAndIgnoresExtraCopies()
        {
            _items.Add(new ItemIdentifier("other-item"));
            ViewModel.Tick();
            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Zero);

            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();
            float enabled = ViewModel.RetrofierOpacity.Value;

            _items.Add(new ItemIdentifier("retrofier"));
            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();

            Assert.That(enabled, Is.Positive);
            Assert.That(ViewModel.RetrofierOpacity.Value, Is.EqualTo(enabled));
            Assert.That(ViewModel.CurrentEffect.Value, Is.Null);
        }

        [Test]
        public void Initialize_ReadsExistingInventory()
        {
            _items.Add(new ItemIdentifier("retrofier"));

            using EffectsViewModel model = new EffectsViewModel(_entities, _audio);

            Assert.That(model.RetrofierOpacity.Value, Is.Positive);
        }

        [Test]
        public void StatusEffects_RemainIndependent()
        {
            IEffectOwnerBehaviour effects = _player
                .GetBehaviour<IEffectOwnerBehaviour>();

            effects.Entity.Returns(_player);
            effects.Effects.Returns(new Dictionary<EffectType, EffectEntry>
            {
                [EffectType.Burning] = new EffectEntry(0, 5, null),
                [EffectType.Flashed] = new EffectEntry(0, 5, null)
            });
            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick(10, 0.1f);

            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Positive);
            Assert.That(ViewModel.CurrentEffect.Value,
                Is.EqualTo(EffectType.Burning));
            Assert.That(ViewModel.FlashProgress.Value, Is.InRange(0f, 1f));
            Assert.That(ViewModel.ActiveEffects.Value,
                Does.Contain(EffectType.Flashed));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PossessionChange_ResetsThenReflectsNewInventory(bool ownsItem)
        {
            IEntityBehaviour next = Substitute.For<IEntityBehaviour>();
            List<ItemIdentifier> nextItems = new();
            List<float> updates = new();

            next.IsValid.Returns(true);
            next.GetBehaviour<IItemOwnerBehaviour>().Items.Returns(nextItems);
            if (ownsItem) nextItems.Add(new ItemIdentifier("retrofier"));
            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();
            ViewModel.RetrofierOpacity.Bind(updates.Add);
            updates.Clear();

            _entities.Changed += Raise.Event<PossessedEntityChanged>(_player, next);

            Assert.That(updates[0], Is.Zero);
            Assert.That(
                ViewModel.RetrofierOpacity.Value > 0, Is.EqualTo(ownsItem));
        }

        [Test]
        public void PossessionLost_ClearsImmediatelyAndIgnoresPreviousInventory()
        {
            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();

            _entities.Changed += Raise.Event<PossessedEntityChanged>(_player, null);
            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Zero);

            ViewModel.Tick();
            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Zero);
        }

        [Test]
        public void InvalidPossessionAndInventoryRemoval_ClearOpacity()
        {
            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();
            _items.Clear();
            ViewModel.Tick();
            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Zero);

            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();
            _player.IsValid.Returns(false);
            ViewModel.Tick();
            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Zero);
        }

        [Test]
        public void Dispose_ClearsOpacityAndUnsubscribesPossession()
        {
            _items.Add(new ItemIdentifier("retrofier"));
            ViewModel.Tick();

            ViewModel.Dispose();
            _entities.Changed += Raise.Event<PossessedEntityChanged>(null, _player);

            Assert.That(ViewModel.RetrofierOpacity.Value, Is.Zero);
        }
    }
}
