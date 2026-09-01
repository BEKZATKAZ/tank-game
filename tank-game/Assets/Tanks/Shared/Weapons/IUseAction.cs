namespace Tanks.Shared.Weapons
{
    public interface IUseAction
    {
        void Use(IWeaponHolderBehaviour owner, ShotId id);
    }

    public class NoUseAction : IUseAction
    {
        public void Use(IWeaponHolderBehaviour owner, ShotId id) { }
    }
}
