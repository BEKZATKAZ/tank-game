using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items;
using Tanks.Localization;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.ItemTagline;
using UnityEngine;

namespace Tanks.UI.Tests.Gameplay
{
    public class ItemTaglineTests : ViewModelTestBase<ItemTaglineViewModel>
    {
        private IClientItemDatabase _clientItemDatabase;
        private ILocalizationDatabase _localization;

        protected override ItemTaglineViewModel CreateViewModel()
        {
            _clientItemDatabase = Substitute.For<IClientItemDatabase>();
            _localization = Substitute.For<ILocalizationDatabase>();

            return new ItemTaglineViewModel(
                Substitute.For<IMessageBus>(), _clientItemDatabase, _localization);
        }

        [Test]
        public void Handle_UpdatesTagline()
        {
            ItemIdentifier id = new ItemIdentifier("test-item");
            IClientItemConfig item = Substitute.For<IClientItemConfig>();

            item.Identifier.Returns(id);
            item.AccentColor.Returns(Color.yellow);
            _clientItemDatabase.Get(id).Returns(item);

            IItemOwnerBehaviour owner = Substitute.For<IItemOwnerBehaviour>();
            owner.Entity.IsPossessed.Returns(false);

            // Does nothing when the entity is not possessed
            ViewModel.Handle(new ItemPickedUp { Owner = owner, ItemId = id }, null);
            _clientItemDatabase.DidNotReceiveWithAnyArgs().Get(id);

            // Now does the logic
            owner.Entity.IsPossessed.Returns(true);
            ViewModel.Handle(new ItemPickedUp { Owner = owner, ItemId = id }, null);

            Assert.That(ViewModel.AccentColor.Value, Is.EqualTo(Color.yellow));
        }
    }
}
