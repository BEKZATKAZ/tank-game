using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Helpers;
using Tanks.Server.Entities;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Generation;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Tests
{
    public sealed class DangerZoneTests
    {
        private IEntityService _entityService;
        private IMessageBus _bus;
        private DangerZone _component;

        [SetUp]
        public void Setup()
        {
            _entityService = Substitute.For<IEntityService>();
            _bus = Substitute.For<IMessageBus>();

            _component = new DangerZone(_entityService, _bus);
            _component.Initialize(Substitute.For<IDangerZoneBehaviour>());

            _component.Behaviour.Damage.Returns(10);
            _component.Behaviour.StartDelay.Returns(10);
            _component.Behaviour.ShrinkDuration.Returns(30);
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Tick_WaitsForDelay_StartsAfter()
        {
            _component.Handle(new TilemapGenerated
            {
                Bounds = new Bounds(new Vector3(5, 3), new Vector3(10, 6))
            }, null);

            // Did not start yet
            _component.Tick(1, 1000);
            _component.Behaviour.DidNotReceiveWithAnyArgs().SetCurrentState(Arg.Any<Rect>());

            // Here it starts
            _component.Handle(new GameStartedFromServer { StartDelaySeconds = 1 }, null);

            for (int i = 0; i < 10; i++) _component.Tick(i + 2, tickDelta: 1);

            _component.Behaviour
                .DidNotReceiveWithAnyArgs()
                .StartShrinkingClientRpc(Arg.Any<DangerZoneStarted>());

            _component.Tick(12, tickDelta: 0.5f); // Early
            _component.Tick(13, tickDelta: 1.5f); // Starts shrinking
            _component.Tick(14, tickDelta: 1.5f); // Next shrink step

            _component.Behaviour
                .Received(1)
                .StartShrinkingClientRpc(Arg.Is<DangerZoneStarted>(x =>
                    x.ServerTick == 13 &&
                    x.ServerDuration == _component.Behaviour.ShrinkDuration));

            _component.Behaviour.SetCurrentState(Arg.Any<Rect>());
        }

        [Test]
        public void Tick_DamagesEntities()
        {
            _component.Handle(new TilemapGenerated
            {
                Bounds = new Bounds(new Vector3(5, 3), new Vector3(10, 6))
            }, null);

            _component.Handle(new GameStartedFromServer { StartDelaySeconds = 1 }, null);
            _component.Tick(15, tickDelta: 20); // +20 seconds

            IEntityBehaviour insideSafeArea = CreateEntity(new float2(4.5f, 2.5f));
            IEntityBehaviour outsideSafeArea = CreateEntity(new float2(0.5f, 2.5f));

            _entityService.GetAllEntities()
                .Returns(new IEntityBehaviour[] { insideSafeArea, outsideSafeArea });

            _component.Tick(16, 1); // +1 second

            insideSafeArea.GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>()
                .DidNotReceiveWithAnyArgs()
                .Damage(Arg.Any<PositiveFloat>());

            outsideSafeArea.GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>()
                .Received(1)
                .Damage(10, Arg.Any<IEntityBehaviour>(), Arg.Any<HealthEventId>());

            // Damages every (1 / tickDelta)
            _component.Tick(17, 0.05f);
            _component.Tick(18, 0.05f);
            _component.Tick(19, 0.05f);
            _component.Tick(20, 0.05f); // Damages

            outsideSafeArea.GetBehaviour<IHealthBehaviour>()
                .Require<IAuthoritativeHealth>()
                .Received(2)
                .Damage(10, Arg.Any<IEntityBehaviour>(), Arg.Any<HealthEventId>());
        }

        private IEntityBehaviour CreateEntity(float2 position)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            entity.Transform.WorldPosition = position;
            return entity;
        }
    }
}