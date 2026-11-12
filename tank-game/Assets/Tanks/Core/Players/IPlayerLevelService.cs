namespace Tanks.Core.Players
{
    public interface IPlayerLevelService
    {
        int RequiredXP(int level);
        PlayerLevel AddXP(PlayerLevel level, int xp);
    }
}
