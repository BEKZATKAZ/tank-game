using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using System.Linq;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class MissingTextureTests : ItemBehaviourTestBase<MissingTextureBehaviour>
    {
        private IDamageService _damageService;

        protected override MissingTextureBehaviour CreateBehaviour()
        {
            _damageService = Substitute.For<IDamageService>();
            return new MissingTextureBehaviour(_damageService);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void FirstPickUp_DoesNothing(int count)
        {
            MockOwner.Items.Returns(Enumerable.Range(0, count).Select(_ => Config.Identifier));
            Behaviour.OnPickup(MockOwner);
            _damageService.DidNotReceiveWithAnyArgs().Damage(default, default);
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(10)]
        public void SecondOrFurtherPickUp_Kills(int count)
        {
            MockOwner.Items.Returns(Enumerable.Range(0, count).Select(_ => Config.Identifier));
            Behaviour.OnPickup(MockOwner);

            _damageService
                .Received(1)
                .Damage(float.MaxValue, MockOwner.Entity, MockOwner.Entity, includeStats: false);
        }
    }
}
