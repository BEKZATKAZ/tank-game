using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Entities.Health;
using Tanks.Client.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Lag;
using Tanks.Shared.Stats;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public class ProjectileMovementPredictionTests
    {
        private ProjectileMovementPrediction _component;
        private IProjectilePoolService _pool;
        private IProjectileBehaviour _behaviour;
        private IProjectileConfig _config;
        private IStatOwnerBehaviour _statOwner;
        private IDamage _damage;
        private IHealthBehaviour _healthBehaviour;
        private IPredictedHealth _predictedHealth;

        [SetUp]
        public void Setup()
        {
            _pool = Substitute.For<IProjectilePoolService>();
            _behaviour = Substitute.For<IProjectileBehaviour>();
            _config = Substitute.For<IProjectileConfig>();
            _statOwner = Substitute.For<IStatOwnerBehaviour>();
            _damage = Substitute.For<IDamage>();
            _healthBehaviour = Substitute.For<IHealthBehaviour>();
            _predictedHealth = Substitute.For<IPredictedHealth>();

            _behaviour.Config.Returns(_config);
            _behaviour.Damage.Returns(_damage);

            _behaviour.Owner.GetBehaviour<IStatOwnerBehaviour>().Returns(_statOwner);
            _healthBehaviour.Require<IPredictedHealth>().Returns(_predictedHealth);

            _component = new ProjectileMovementPrediction(
                _pool, Substitute.For<ILagCompensationGateway>());

            _component.Initialize(_behaviour);
            _component.Transform.WorldScale.Returns(new float2(1, 1));
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Tick_SimulatesProjectile()
        {
            _config.LifeTime.Returns(10);
            _component.Tick(1, 0.1f);

            _behaviour.Received().Simulate(
                Arg.Any<LaunchCallbacks>(),
                _component.SimulatedTransform,
                0.1f);
        }

        [Test]
        public void Tick_WhenLifetimeExpired_ReturnsProjectile()
        {
            _config.LifeTime.Returns(0);
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
            _config.LifeTime.Returns(10);

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
            _config.LifeTime.Returns(10);
            _config.MaxRicochetCount.Returns((ushort)1);

            SimulateWithRicochets(2);
            _component.Tick(10, 0.1f);

            _pool.Received(1).Return(_behaviour.Id);
        }

        [Test]
        public void Tick_WhenRicochetWithinLimit_ForwardsRicochet()
        {
            _config.LifeTime.Returns(10);
            _config.MaxRicochetCount.Returns((ushort)2);

            SimulateWithRicochets(1);
            _component.Tick(10, 0.1f);

            _behaviour.Received(1).OnRicochet(Arg.Any<float2>(), Arg.Any<float2>());
            _pool.DidNotReceive().Return(_behaviour.Id);
        }

        [TestCase(0.1f)]
        [TestCase(0.5f)]
        [TestCase(2)]
        public void Tick_WhenDamageableHit_DealsModifiedDamage(float tickDelta)
        {
            IEntityBehaviour from = Substitute.For<IEntityBehaviour>();

            _behaviour.Owner.Entity.Returns(from);
            _config.LifeTime.Returns(10);

            // 'Evaluate' takes 'elapsed', 'tickDelta' is elapsed
            _damage.Evaluate(tickDelta).Returns(25);
            _statOwner.GetStat(StatType.Damage, 25).Returns(40);

            _component.Handle(new ProjectileLaunched { Damage = _damage });

            SimulateWithDamageableHit(tickDelta);
            _component.Tick(1, tickDelta);

            _predictedHealth.Received(1).Damage(40, from, Arg.Any<HealthEventId>());
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
