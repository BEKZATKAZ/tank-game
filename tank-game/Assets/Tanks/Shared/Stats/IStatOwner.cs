namespace Tanks.Shared.Stats
{
    public enum StatType
    {
        Damage,
        Speed
    }

    public interface IStatOwner
    {
        float GetStat(StatType type, float baseValue);

        void AddStat(
            StatType type, StatValueType valueType, float value, float duration = -1);
    }
}
