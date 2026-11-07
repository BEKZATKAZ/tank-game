using Tanks.Client.Items;
using Tanks.Shared.Items.Data;

namespace Tanks.UI.Items
{
    public sealed class ItemDescriptionData
    {
        public ItemDescriptionData(
            IItemConfig config, IClientItemConfig clientConfig,
            string name, string description, string tierName)
        {
            Config = config;
            ClientConfig = clientConfig;
            Name = name;
            Description = description;
            TierName = tierName;
        }

        public IItemConfig Config { get; }
        public IClientItemConfig ClientConfig { get; }
        public string Name { get; }
        public string Description { get; }
        public string TierName { get; }
    }
}
