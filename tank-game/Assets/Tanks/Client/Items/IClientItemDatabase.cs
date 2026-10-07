using Cysharp.Threading.Tasks;
using Tanks.Shared.Items;

namespace Tanks.Client.Items
{
    public interface IClientItemDatabase
    {
        IClientItemConfig Get(ItemIdentifier id);
        UniTask WaitForInitializationAsync();
    }
}
