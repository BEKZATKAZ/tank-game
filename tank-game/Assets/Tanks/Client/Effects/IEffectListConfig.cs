using System.Collections.Generic;
using Tanks.Shared.Effects;

namespace Tanks.Client.Effects
{
    public interface IEffectListConfig
    {
        IReadOnlyDictionary<EffectType, EffectConfig> Effects { get; }
    }
}
