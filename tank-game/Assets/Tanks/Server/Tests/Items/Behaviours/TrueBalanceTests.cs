using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Tanks.Server.Items.Behaviours.OnFire;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public class TrueBalanceTests : ItemBehaviourTestBase<TrueBalanceBehaviour>
    {
        private IEntityService _entityService;
        private IDamageService _damageService;

        protected override void InitializeConfig()
        {
            AddItemStat("heal", new ItemStat { Value = 5 });
        }

        protected override TrueBalanceBehaviour CreateBehaviour()
        {
            _entityService = Substitute.For<IEntityService>();
            _damageService = Substitute.For<IDamageService>();
            return new TrueBalanceBehaviour(_entityService, _damageService);
        }

        [Test]
        public void OnFire_HealsOthers()
        {
            IEntityBehaviour[] allEntities = Enumerable
                .Range(0, 10)
                .Select(i =>
                {
                    IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
                    IItemOwnerBehaviour itemOwner = entity.GetBehaviour<IItemOwnerBehaviour>();

                    if (i < 5)
                    {
                        IEnumerable<ItemIdentifier> items = new ItemIdentifier[] { Config.Identifier };
                        itemOwner.Items.Returns(items);
                    }

                    return entity;
                })
                .ToArray();

            _entityService.GetAllEntities().Returns(allEntities);

            Behaviour.OnFire(MockOwner, default);

            uint healAmount = (uint)Config.GetStat("heal").Value;

            foreach (IEntityBehaviour entity in allEntities)
            {
                IItemOwnerBehaviour itemOwner = entity.GetBehaviour<IItemOwnerBehaviour>();
                bool shouldAffect = itemOwner.Items.Contains(Config.Identifier) == false;

                if (shouldAffect) _damageService.Received(1).Heal(healAmount, entity);
                else _damageService.DidNotReceive().Heal(healAmount, entity);
            }
        }
    }
}
