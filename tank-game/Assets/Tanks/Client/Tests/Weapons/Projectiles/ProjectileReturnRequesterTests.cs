using NSubstitute;
using NUnit.Framework;
using System.Linq;
using Tanks.Client.Weapons.Projectiles;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Data;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Client.Tests.Weapons.Projectiles
{
    public class ProjectileReturnRequesterTests
    {
        private ProjectileReturnRequester _component;

        [SetUp]
        public void Setup()
        {
            _component = new ProjectileReturnRequester();
            _component.Initialize(Substitute.For<IProjectilePoolService>());
        }

        [Test]
        public void Request_Invalid_DoesNotAddPending()
        {
            ProjectileId id = new ProjectileId(new ShotId(1), 1);
            _component.Handle(new ProjectileReturnRequested { Id = id });
            Assert.False(_component.Pending.Any(p => p.Id == id));
        }

        [Test]
        public void Request_Valid_AddsPending()
        {
            ProjectileId id = new ProjectileId(new ShotId(1), 1);
            IProjectileBehaviour pendingProjectile = Substitute.For<IProjectileBehaviour>();

            _component.Behaviour
                .TryGetProjectile(id, out Arg.Any<IProjectileBehaviour>())
                .Returns(call =>
                {
                    call[1] = pendingProjectile;
                    return true;
                });

            _component.Handle(new ProjectileReturnRequested { Id = id });
            Assert.True(_component.Pending.Any(p => p.Id == id));

            _component.Handle(new ProjectileReturnRequestRejected { Id = id });
            Assert.False(_component.Pending.Any(p => p.Id == id));

            pendingProjectile.Received(1).Launch(pendingProjectile.Owner, id);
        }
    }
}
