using Tanks.Shared.Items;

namespace Tanks.Client.Items
{
    public interface IClientItemDatabase
    {
        IClientItemConfig Get(ItemIdentifier id);
    }
}
