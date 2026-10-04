using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Lag;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public class ProjectileMovementPredictionTests
    {
        private ProjectileMovementPrediction _component;
        private IProjectilePoolService _pool;
        private IProjectileBehaviour _behaviour;
        private IHealthBehaviour _healthBehaviour;
        private ILagCompensationGateway _lagCompensation;

        [SetUp]
        public void Setup()
        {
            _pool = Substitute.For<IProjectilePoolService>();
            _behaviour = Substitute.For<IProjectileBehaviour>();
            _healthBehaviour = Substitute.For<IHealthBehaviour>();
            _lagCompensation = Substitute.For<ILagCompensationGateway>();

            _component = new ProjectileMovementPrediction(_pool, _lagCompensation);
            _component.Initialize(_behaviour);
            _component.Transform.WorldScale.Returns(new float2(1, 1));

            _behaviour.Id.Returns(new ProjectileId(new ShotId(1900), 5));
            _healthBehaviour.Entity.Id.Returns(new EntityId(1200));
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Tick_SimulatesProjectile()
        {
            _behaviour.LifeTime.Returns(10);
            _component.Tick(1, 0.1f);

            _behaviour.Received().Simulate(
                Arg.Any<LaunchCallbacks>(),
                _component.SimulatedTransform,
                0.1f);
        }

        [Test]
        public void Tick_WhenLifetimeExpired_ReturnsProjectile()
        {
            _behaviour.LifeTime.Returns(0);
            _component.Tick(10, 0.1f);
            _pool.Received(1).Return(_behaviour.Id);

            _behaviour.DidNotReceiveWithAnyArgs().Simulate(
                Arg.Any<LaunchCallbacks>(),
                Arg.Any<ITransform>(),
                Arg.Any<float>());
        }

        [Test]
        public void Update_CopiesSimulatedTransformToRealTransform()
        {
            _behaviour.LifeTime.Returns(10);

            _behaviour
                .When(x => x.Simulate(
                    Arg.Any<LaunchCallbacks>(),
                    Arg.Any<ITransform>(),
                    Arg.Any<float>()))
                .Do(x =>
                {
                    ITransform simulated = x.Arg<ITransform>();
                    simulated.WorldPosition = new float2(4, 3);
                    simulated.WorldRotation = Angle.FromRadians(1);
                });

            _component.Tick(1, 0.1f);
            _component.Update(0.1f);

            Assert.AreEqual(new float2(4, 3), _component.SimulatedTransform.WorldPosition);
            Assert.AreEqual(Angle.FromRadians(1), _component.SimulatedTransform.WorldRotation);

            _component.Transform.Received().WorldPosition = Arg.Any<float2>();
            _component.Transform.Received().WorldRotation = Arg.Any<Angle>();
        }

        [Test]
        public void Handle_Launch_InitializesPredictionFromTransform()
        {
            _component.Transform.WorldPosition.Returns(new float2(10, 20));
            _component.Transform.WorldRotation.Returns(Angle.FromRadians(2));

            _component.Handle(new ProjectileLaunched());

            _behaviour.DidNotReceiveWithAnyArgs().Simulate(
                Arg.Any<LaunchCallbacks>(),
                Arg.Any<ITransform>(),
                Arg.Any<float>());
        }

        [Test]
        public void Tick_WhenRicochetLimitExceeded_ReturnsProjectile()
        {
            _behaviour.LifeTime.Returns(10);
            _behaviour.MaxRicochetCount.Returns((ushort)1);

            SimulateWithRicochets(2);
            _component.Tick(10, 0.1f);

            _pool.Received(1).Return(_behaviour.Id);
        }

        [Test]
        public void Tick_WhenRicochetWithinLimit_ForwardsRicochet()
        {
            _behaviour.LifeTime.Returns(10);
            _behaviour.MaxRicochetCount.Returns((ushort)2);

            SimulateWithRicochets(1);
            _component.Tick(10, 0.1f);

            _behaviour.Received(1).OnRicochet(Arg.Any<float2>(), Arg.Any<float2>());
            _pool.DidNotReceive().Return(_behaviour.Id);
        }

        [TestCase(0.1f)]
        [TestCase(0.5f)]
        [TestCase(2)]
        public void Tick_WhenDamageableHit_RunsCallback(float tickDelta)
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();

            _behaviour.Owner.Entity.Returns(from);
            _behaviour.LifeTime.Returns(10);

            _component.Handle(new ProjectileLaunched());

            SimulateWithDamageableHit(tickDelta);
            _component.Tick(1, tickDelta);
            _behaviour.Received(1).OnDamageableHit(_healthBehaviour);

            _lagCompensation.RequestHitServerRpc(Arg.Is<HitRequest>(x =>
                x.ClientTick == 1 &&
                x.ProjectileId == _behaviour.Id &&
                x.TargetId == _healthBehaviour.Entity.Id));

            _pool.Received(1).Return(_behaviour.Id);
        }

        private void SimulateWithRicochets(int count)
        {
            _behaviour
                .When(x => x.Simulate(
                    Arg.Any<LaunchCallbacks>(),
                    Arg.Any<ITransform>(),
                    Arg.Any<float>()))
                .Do(x =>
                {
                    LaunchCallbacks callbacks = x.Arg<LaunchCallbacks>();

                    for (int i = 0; i < count; i++)
                    {
                        callbacks.OnRicochet(float2.zero, new float2(1, 0));
                    }
                });
        }

        private void SimulateWithDamageableHit(float expectedDeltaTime)
        {
            _behaviour
                .When(x => x.Simulate(
                    Arg.Any<LaunchCallbacks>(),
                    _component.SimulatedTransform,
                    expectedDeltaTime))
                .Do(x =>
                {
                    LaunchCallbacks callbacks = x.Arg<LaunchCallbacks>();
                    callbacks.OnDamageableHit(_healthBehaviour);
                });
        }
    }
}
