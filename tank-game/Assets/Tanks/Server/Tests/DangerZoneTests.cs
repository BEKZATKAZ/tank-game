using NSubstitute;
using NSubstitute.ReceivedExtensions;
using NUnit.Framework;
using Tanks.Core.Lobbies.Sessions;
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
        private ISessionSettingsService _settings;
        private IDamageService _damageService;
        private DangerZone _component;

        [SetUp]
        public void Setup()
        {
            _entityService = Substitute.For<IEntityService>();
            _bus = Substitute.For<IMessageBus>();
            _damageService = Substitute.For<IDamageService>();

            _settings = Substitute.For<ISessionSettingsService>();
            _settings.GetSettings().DangerZone.Returns(new DangerZoneSettings
            {
                StartDelaySeconds = 10,
                DurationSeconds = 30,
                DamagePerSecond = 10
            });

            _component = new DangerZone(_entityService, _bus, _settings, _damageService);
            _component.Initialize(Substitute.For<IDangerZoneBehaviour>());
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
                .StartShrinkingClientRpc(Arg.Is<DangerZoneStarted>(x => x.ServerTick == 13));

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

            _damageService
                .DidNotReceive()
                .Damage(10, insideSafeArea, null, Arg.Any<HealthEventId>(), false);

            _damageService
                .Received(1)
                .Damage(10, outsideSafeArea, null, Arg.Any<HealthEventId>(), false);

            // Damages every (1 / tickDelta)
            _component.Tick(17, 0.05f);
            _component.Tick(18, 0.05f);
            _component.Tick(19, 0.05f);
            _component.Tick(20, 0.05f); // Damages

            _damageService
                .Received(2)
                .Damage(10, outsideSafeArea, null, Arg.Any<HealthEventId>(), false);
        }

        private IEntityBehaviour CreateEntity(float2 position)
        {
            IEntityBehaviour entity = Substitute.For<IEntityBehaviour>();
            entity.Transform.WorldPosition = position;
            return entity;
        }
    }
}
