using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;
using Unity.Mathematics;

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

                    entity
                        .GetBehaviour<IHealthBehaviour>().ServerState.Value
                        .Returns(new HealthState(100, 100));

                    return entity;
                })
                .ToArray();

            _entityService.GetAllEntities().Returns(entities);

            Behaviour.OnPickup(MockOwner);
            uint expectedDamage = (uint)Config.GetStat("damage").Value;

            _damageService.Received(1).Damage(
                expectedDamage,
                Arg.Is<IEntityBehaviour>(x => entities.Contains(x)),
                MockOwner.Entity, Arg.Any<HealthEventId>(), false);
        }

        [TestCase(59)]
        [TestCase(10)]
        [TestCase(2)]
        public void PickUp_Lethal_LeavesOneHP(int remainingHealth)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();

            entity.IsValid.Returns(true);
            entity
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(remainingHealth, 100));

            _entityService.GetAllEntities().Returns(new IEntityBehaviour[] { entity });
            Behaviour.OnPickup(MockOwner);

            uint expectedDamage = (uint)remainingHealth - 1;

            _damageService.Received(1).Damage(
                expectedDamage, entity, MockOwner.Entity, Arg.Any<HealthEventId>(), false);
        }

        [Test]
        public void PickUp_OneHP_DoesNothing()
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();

            entity.IsValid.Returns(true);
            entity
                .GetBehaviour<IHealthBehaviour>().ServerState.Value
                .Returns(new HealthState(1, 100));

            _entityService.GetAllEntities().Returns(new IEntityBehaviour[] { entity });
            Behaviour.OnPickup(MockOwner);

            _damageService.DidNotReceiveWithAnyArgs().Damage(
                default, default, default, default, default);
        }
    }
}
