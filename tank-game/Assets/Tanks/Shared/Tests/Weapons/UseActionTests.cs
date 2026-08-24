using NUnit.Framework;
using Tanks.Shared.Weapons;

namespace Tanks.Shared.Tests.Weapons
{
    public class UseActionTests
    {
        [Test]
        public void NoAction_DoesNothing()
        {
            IUseAction action = new NoUseAction();
            action.Use(null, new ShotId(10));
        }
    }
}
