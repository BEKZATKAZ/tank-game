using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Weapons;
using Tanks.Shared.Weapons;
using Tanks.Shared.Weapons.Projectiles;

namespace Tanks.Server.Tests.Weapons
{
    public class ProjectileReturnValidatorTests
    {
        private ProjectileReturnValidator _component;

        [SetUp]
        public void Setup()
        {
            _component = new ProjectileReturnValidator();
            _component.Initialize(Substitute.For<IProjectilePoolService>());
        }

        [Test]
        public void Handle_WithProjectile_RejectsReturn()
        {
            ProjectileId id = new ProjectileId(new ShotId(5), 3);
            IProjectileBehaviour projectile = Substitute.For<IProjectileBehaviour>();

            _component.Behaviour
                .TryGetProjectile(id, out Arg.Any<IProjectileBehaviour>())
                .Returns(call =>
                {
                    call[1] = projectile;
                    return true;
                });

            _component.Handle(new ProjectileReturnRequested { Id = id });
            _component.Behaviour
                .Received(1)
                .RejectReturnClientRpc(Arg.Is<ProjectileReturnRequestRejected>(r => r.Id == id));
        }

        [Test]
        public void Handle_NoProjectileFound_DoesNothing()
        {
            ProjectileId id = new ProjectileId(new ShotId(2), 1);

            _component.Handle(new ProjectileReturnRequested { Id = id });
            _component.Behaviour
                .DidNotReceiveWithAnyArgs()
                .RejectReturnClientRpc(Arg.Any<ProjectileReturnRequestRejected>());
        }
    }
}
