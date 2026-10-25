using System.Collections.Generic;

namespace Tanks.Shared.Stats
{
    public interface IStatOwnerBehaviour : INetworkBehaviourExtended
    {
        float GetStat(StatType type, float baseValue);
        float GetDiminishingReturns(StatType type, float value);
        void GetStatTimers(StatType type, List<StatTimer> results);

        bool HasStat(StatId id);

        StatId AddStat(
            StatType type, StatValueType valueType, float value, float duration = -1);

        void OverrideStat(
            StatId id, StatType type, StatValueType valueType,
            float value, float duration = -1);

        void RemoveStat(StatId id);
    }
}
