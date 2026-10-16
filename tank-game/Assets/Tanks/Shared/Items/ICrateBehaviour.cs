using System.Collections.Generic;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Items
{
    public interface ICrateBehaviour : INetworkBehaviourExtended
    {
        IReadOnlyDictionary<ItemTier, float> Chances { get; }
    }
}
