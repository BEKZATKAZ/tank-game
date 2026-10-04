using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons.Projectiles;
using Tanks.Shared.Adapters;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Server.Tests.Weapons.Projectiles
{
    public class ProjectileMovementTests
    {
        private ProjectileMovement _component;
        private IProjectilePoolService _pool;
        private IProjectileBehaviour _behaviour;
        private IHealthBehaviour _healthBehaviour;
        private ITransform _transform;

        [SetUp]
        public void Setup()
        {
            _pool = Substitute.For<IProjectilePoolService>();
            _behaviour = Substitute.For<IProjectileBehaviour>();
            _healthBehaviour = Substitute.For<IHealthBehaviour>();
            _transform = Substitute.For<ITransform>();

            _component = new ProjectileMovement(_pool);
            _component.Initialize(_behaviour);
            _component.Transform.Returns(_transform);
        }

        [Test]
        public void Tick_WithInvalidTransform_DoesNothing()
        {
            _transform.IsValid.Returns(false);
            Tick();

            _behaviour.DidNotReceive().Simulate(
                Arg.Any<LaunchCallbacks>(),
                Arg.Any<ITransform>(),
                Arg.Any<float>());

            _pool.DidNotReceive().Return(_behaviour.Id);
            _pool.DidNotReceive().RequestReconciliation(
                Arg.Any<IProjectileBehaviour>(),
                Arg.Any<ProjectileSnapshotFromServer>());
        }

        [Test]
        public void Tick_WhenLifetimeExpires_ReturnsProjectileOnNextTick()
        {
            _transform.IsValid.Returns(true);
            _behaviour.LifeTime.Returns(1);

            Launch();
            Tick(deltaTime: 1);

            _behaviour.Received(1).Simulate(Arg.Any<LaunchCallbacks>(), _transform, 1);
            _pool.DidNotReceive().Return(_behaviour.Id);

            Tick();

            _behaviour.Received(1).Simulate(
                Arg.Any<LaunchCallbacks>(), Arg.Any<ITransform>(), Arg.Any<float>());

            _pool.Received(1).Return(_behaviour.Id);
        }

        [Test]
        public void Tick_SimulatesProjectile()
        {
            _transform.IsValid.Returns(true);
            _behaviour.LifeTime.Returns(10);

            Launch();
            Tick();

            _behaviour.Received(1).Simulate(Arg.Any<LaunchCallbacks>(), _transform, 0.1f);
        }

        [Test]
        public void Tick_RequestsReconciliation()
        {
            _transform.IsValid.Returns(true);
            _behaviour.LifeTime.Returns(10f);

            Launch();
            Tick(42, 0.25f);

            _pool.Received(1).RequestReconciliation(
                _behaviour,
                Arg.Is<ProjectileSnapshotFromServer>(x =>
                    x.ServerPosition.Equals(_transform.WorldPosition) &&
                    x.ServerRotation.Equals(_transform.WorldRotation) &&
                    x.ServerTick == 42));
        }

        [Test]
        public void Tick_WhenRicochetLimitExceeded_ReturnsProjectile()
        {
            _transform.IsValid.Returns(true);
            _behaviour.LifeTime.Returns(10);
            _behaviour.MaxRicochetCount.Returns((ushort)1);

            SimulateWithRicochets(2);
            Launch();
            Tick();

            _pool.Received(1).Return(_behaviour.Id);
            _pool.DidNotReceive().RequestReconciliation(
                Arg.Any<IProjectileBehaviour>(),
                Arg.Any<ProjectileSnapshotFromServer>());
        }

        [Test]
        public void Tick_WhenRicochetLimitReached_DoesNotReturnProjectile()
        {
            _transform.IsValid.Returns(true);
            _behaviour.LifeTime.Returns(10);
            _behaviour.MaxRicochetCount.Returns((ushort)2);

            SimulateWithRicochets(2);
            Launch();
            Tick();

            _pool.DidNotReceive().Return(_behaviour.Id);
            _pool.Received(1).RequestReconciliation(
                _behaviour, Arg.Any<ProjectileSnapshotFromServer>());
        }

        [Test]
        public void Tick_WhenDamageableIsHit_RunsCallback()
        {
            _transform.IsValid.Returns(true);
            _behaviour.LifeTime.Returns(10);

            SimulateWithDamageableHit();
            Launch();
            Tick();

            _behaviour.Received(1).OnDamageableHit(_healthBehaviour);
            _pool.Received(1).Return(_behaviour.Id);
        }

        private void Launch()
        {
            _component.Handle(new ProjectileLaunched());
        }

        private void Tick(int tick = 1, float deltaTime = 0.1f)
        {
            _component.Tick(tick, deltaTime);
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

        private void SimulateWithDamageableHit()
        {
            _behaviour
                .When(x => x.Simulate(
                    Arg.Any<LaunchCallbacks>(),
                    Arg.Any<ITransform>(),
                    Arg.Any<float>()))
                .Do(x =>
                {
                    LaunchCallbacks callbacks = x.Arg<LaunchCallbacks>();
                    callbacks.OnDamageableHit(_healthBehaviour);
                });
        }
    }
}
