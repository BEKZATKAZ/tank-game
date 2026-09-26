using NSubstitute;
using NUnit.Framework;
using Tanks.Shared;
using Tanks.Shared.Entities;
using Tanks.Shared.Messaging;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Lag
{
    public class LagCompensationServiceTests
    {
        private LagCompensationService _service;
        private IHitSimulator _simulator;
        private IMessageBus _bus;

        [SetUp]
        public void Setup()
        {
            _simulator = Substitute.For<IHitSimulator>();
            _bus = Substitute.For<IMessageBus>();
            _service = new LagCompensationService(_simulator, _bus);
        }

        [TearDown]
        public void TearDown() => _service.Dispose();

        [Test]
        public void Constructor_RegistersHandler()
        {
            _bus.Received().RegisterHandler<GameStartedFromServer>(_service);
        }

        [Test]
        public void Dispose_UnregistersHandler()
        {
            _service.Dispose();

            _bus.Received().UnregisterHandler<GameStartedFromServer>(_service);
        }

        [Test]
        public void GetEntitySnapshot_ReturnsCapturedSnapshot()
        {
            EntityId id = default;
            EntitySnapshot snapshot = default;

            CaptureEntity(id, 10, snapshot);
            Assert.That(_service.GetEntitySnapshot(id, 10), Is.EqualTo(snapshot));
        }

        [Test]
        public void GetEntitySnapshot_ReturnsDefault_WhenNotCaptured()
        {
            Assert.That(
                _service.GetEntitySnapshot(default, 10),
                Is.EqualTo(default(EntitySnapshot)));
        }

        [Test]
        public void GetProjectileSnapshot_ReturnsCapturedSnapshot()
        {
            ProjectileId id = default;
            ProjectileSnapshot snapshot = default;

            CaptureProjectile(id, 10, snapshot);
            Assert.That(_service.GetProjectileSnapshot(id, 10), Is.EqualTo(snapshot));
        }

        [Test]
        public void GetProjectileSnapshot_ReturnsDefault_WhenNotCaptured()
        {
            Assert.That(
                _service.GetProjectileSnapshot(default, 10),
                Is.EqualTo(default(ProjectileSnapshot)));
        }

        [Test]
        public void IsHitAtTick_ReturnsSimulatorResult()
        {
            var (entityId, projectileId) = CaptureBoth(10);
            _simulator.Overlaps(default, default).ReturnsForAnyArgs(true);

            bool result = _service.IsHitAtTick(entityId, projectileId, 10, out var usedTick);

            Assert.That(result, Is.True);
            Assert.That(usedTick, Is.EqualTo(10));
            _simulator.Received(1).Overlaps(default, default);
        }

        [Test]
        public void IsHitAtTick_ReturnsFalse_WhenEntityDoesNotExist()
        {
            ProjectileId projectileId = default;
            CaptureProjectile(projectileId, 10, default);

            bool result = _service.IsHitAtTick(default, projectileId, 10, out var usedTick);

            Assert.That(result, Is.False);
            Assert.That(usedTick, Is.EqualTo(10));
            _simulator.DidNotReceive().Overlaps(default, default);
        }

        [Test]
        public void IsHitAtTick_ReturnsFalse_WhenProjectileDoesNotExist()
        {
            EntityId entityId = default;
            CaptureEntity(entityId, 10, default);

            bool result = _service.IsHitAtTick(entityId, default, 10, out var usedTick);

            Assert.That(result, Is.False);
            Assert.That(usedTick, Is.EqualTo(10));
            _simulator.DidNotReceive().Overlaps(default, default);
        }

        [Test]
        public void IsHitAtTick_ClampsFutureTick()
        {
            var (entityId, projectileId) = CaptureBoth(10);
            bool result = _service.IsHitAtTick(entityId, projectileId, 100, out var usedTick);

            Assert.That(result, Is.False);
            Assert.That(usedTick, Is.EqualTo(10));
        }

        [Test]
        public void IsHitAtTick_ClampsPastTickToBuffer()
        {
            var (entityId, projectileId) = CaptureBoth(100);
            bool result = _service.IsHitAtTick(entityId, projectileId, 0, out var usedTick);

            Assert.That(result, Is.False);
            Assert.That(usedTick, Is.EqualTo(100 - LagCompensationService.BUFFER_SIZE));
        }

        [Test]
        public void Capture_DoesNotMoveLatestTickBackwards()
        {
            var (entityId, projectileId) = CaptureBoth(100);

            CaptureEntity(entityId, 50, default);
            CaptureProjectile(projectileId, 50, default);

            _service.IsHitAtTick(entityId, projectileId, 200, out var usedTick);

            Assert.That(usedTick, Is.EqualTo(100));
        }

        [Test]
        public void GameStarted_ClearsSnapshots()
        {
            var (entityId, projectileId) = CaptureBoth(10);

            _service.Handle(default, Substitute.For<IMessageSender>());

            Assert.That(
                _service.GetEntitySnapshot(entityId, 10),
                Is.EqualTo(default(EntitySnapshot)));

            Assert.That(
                _service.GetProjectileSnapshot(projectileId, 10),
                Is.EqualTo(default(ProjectileSnapshot)));
        }

        private void CaptureEntity(EntityId id, int tick, EntitySnapshot snapshot)
        {
            _service.CaptureEntity(id, tick, in snapshot);
        }

        private void CaptureProjectile(ProjectileId id, int tick, ProjectileSnapshot snapshot)
        {
            _service.CaptureProjectile(id, default, tick, in snapshot);
        }

        private (EntityId, ProjectileId) CaptureBoth(int tick)
        {
            EntityId entityId = default;
            ProjectileId projectileId = default;

            CaptureEntity(entityId, tick, default);
            CaptureProjectile(projectileId, tick, default);

            return (entityId, projectileId);
        }
    }
}
