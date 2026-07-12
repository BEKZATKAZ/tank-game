using System;

namespace Tanks.Shared.Weapons
{
    public interface IUseAction
    {
        void Use(IWeaponOwner owner, ShotId id);
    }

    public class NoUseAction : IUseAction
    {
        public void Use(IWeaponOwner owner, ShotId id) { }
    }
}
