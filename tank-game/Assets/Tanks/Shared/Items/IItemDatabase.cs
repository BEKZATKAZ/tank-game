using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;

namespace Tanks.Shared.Items
{
    public interface IItemDatabase
    {
        IEnumerable<IItemConfig> Items { get; }

        UniTask WaitForInitializationAsync();
        IItemConfig Get(in ItemIdentifier id);
        IEnumerable<ItemBehaviourBase> GetBehaviours(in ItemIdentifier id);
        IEnumerable<KeyValuePair<ItemIdentifier, ItemBehaviourBase[]>> GetAllBehaviours();
    }
}
