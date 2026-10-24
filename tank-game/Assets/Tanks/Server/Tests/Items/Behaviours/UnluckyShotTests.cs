using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class UnluckyShotTests : ItemBehaviourTestBase<UnluckyShotBehaviour>
    {
        private IEntityService _entityService;
        private IDamageService _damageService;

        protected override void InitializeConfig()
        {
            AddItemStat("damage", new ItemStat { Value = 60 });
        }

        protected override UnluckyShotBehaviour CreateBehaviour()
        {
            _entityService = Substitute.For<IEntityService>();
            _damageService = Substitute.For<IDamageService>();
            return new UnluckyShotBehaviour(_entityService, _damageService);
        }

        [Test]
        public void PickUp_DamagesRandomPlayer()
        {
            IEntityBehaviour[] entities = Enumerable
                .Range(0, 10)
                .Select(_ =>
                {
                    IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
                    entity.IsValid.Returns(true);
                    return entity;
                })
                .ToArray();

            _entityService.GetAllEntities().Returns(entities);

            Behaviour.OnPickup(MockOwner);

            _damageService.Received(1).Damage(
                (uint)Config.GetStat("damage").Value,
                Arg.Is<IEntityBehaviour>(x => entities.Contains(x)),
                MockOwner.Entity,
                Arg.Any<HealthEventId>(),
                false);
        }
    }
}
