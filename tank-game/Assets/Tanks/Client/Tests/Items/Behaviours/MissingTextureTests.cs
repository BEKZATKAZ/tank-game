using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Entities;
using Tanks.Client.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Tests.Items;

namespace Tanks.Client.Tests.Items.Behaviours
{
    public class MissingTextureTests : ItemBehaviourTestBase<MissingTextureBehaviour>
    {
        private IEntityService _entityService;

        protected override MissingTextureBehaviour CreateBehaviour()
        {
            _entityService = Substitute.For<IEntityService>();

            IEntityBehaviour[] allEntities = Enumerable.Range(0, 10)
                .Select(i =>
                {
                    IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();

                    if (i < 5)
                    {
                        entity.GetBehaviour<IHealthBehaviour>().Returns((IHealthBehaviour)null);
                    }

                    return entity;
                })
                .ToArray();

            _entityService.GetAllEntities().Returns(allEntities);

            return new MissingTextureBehaviour(_entityService);
        }

        [Test]
        public void OnPickup_LocalPlayer_EveryoneHides()
        {
            MockOwner.IsLocalPlayer.Returns(true);
            Behaviour.OnPickup(MockOwner);

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                IEntityRenderer renderer = entity.Require<IEntityRenderer>();
                if (renderer is null) continue;
                renderer.Received(1).GetAllSpriteRenderers();
            }
        }

        [Test]
        public void OnPickup_OtherPlayer_SelfHide()
        {
            MockOwner.IsLocalPlayer.Returns(false);
            Behaviour.OnPickup(MockOwner);

            MockOwner.Entity
                .Require<IEntityRenderer>()
                .Received(1)
                .GetAllSpriteRenderers();

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                IEntityRenderer renderer = entity.Require<IEntityRenderer>();
                if (renderer is null) continue;
                renderer.DidNotReceiveWithAnyArgs().GetAllSpriteRenderers();
            }
        }

        [Test]
        public void OnHealthChanged_LocalPlayer_RevealsAllExceptWithThisItem()
        {
            MockOwner.IsLocalPlayer.Returns(true);

            // Some entities have this item
            foreach (IEntityBehaviour entity in _entityService.GetAllEntities().Take(2))
            {
                IItemOwnerBehaviour itemOwner = entity.GetBehaviour<IItemOwnerBehaviour>();
                IEnumerable<ItemIdentifier> items = new ItemIdentifier[] { Config.Identifier };
                itemOwner.Items.Returns(items);
            }

            // Some entities cannot have items
            foreach (IEntityBehaviour entity in _entityService.GetAllEntities().TakeLast(2))
            {
                entity.GetBehaviour<IItemOwnerBehaviour>().Returns(null as IItemOwnerBehaviour);
            }

            Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(0, 100));

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                IEntityRenderer renderer = entity.Require<IEntityRenderer>();
                if (renderer is null) continue;

                IItemOwnerBehaviour itemOwner = entity.GetBehaviour<IItemOwnerBehaviour>();
                bool shouldNotReceive = itemOwner?.Items.Contains(Config.Identifier) ?? true;

                if (shouldNotReceive)
                {
                    renderer.DidNotReceiveWithAnyArgs().GetAllSpriteRenderers();
                    continue;
                }

                renderer.Received(1).GetAllSpriteRenderers();
            }
        }

        [Test]
        public void OnHealthChanged_OtherPlayer_DoesNothing()
        {
            MockOwner.IsLocalPlayer.Returns(false);
            Behaviour.OnHealthChanged(MockOwner, null, default, new HealthState(0, 100));

            foreach (IEntityBehaviour entity in _entityService.GetAllEntities())
            {
                IEntityRenderer renderer = entity.Require<IEntityRenderer>();
                if (renderer is null) continue;
                renderer.DidNotReceiveWithAnyArgs().GetAllSpriteRenderers();
            }
        }
    }
}
