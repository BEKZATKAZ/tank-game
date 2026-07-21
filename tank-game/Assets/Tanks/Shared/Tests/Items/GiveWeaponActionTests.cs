using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Helpers;
using Tanks.Shared.Adapters;
using Tanks.Shared.Items;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;

namespace Tanks.Shared.Tests.Items
{
    public class GiveWeaponActionTests
    {
        private IWeaponFactory _factory;
        private IWeaponConfig _config;
        private IItemOwner _owner;

        [SetUp]
        public void Setup()
        {
            _owner = Substitute.For<IItemOwner>();
            _config = Substitute.For<IWeaponConfig>();
            _factory = Substitute.For<IWeaponFactory>();
            _factory.Create(_config, Arg.Any<ITransform>()).Returns(new Weapon());
        }

        [Test]
        public void Invoke_CreatesWeapon_GivesToOwner()
        {
            IItemAction action = new GiveWeaponAction(_factory.AsRequired(), _config.AsRequired());
            action.Invoke(_owner);
        }
    }
}
