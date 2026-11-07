using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Client.Items;
using Tanks.Core.Repositories;
using Tanks.Shared.Items.Data;

namespace Tanks.UI.Menus.Items
{
    public sealed class ItemCatalogEntry
    {
        public ItemCatalogEntry(IItemConfig config, IClientItemConfig clientConfig)
        {
            Config = config;
            ClientConfig = clientConfig;
        }

        public IItemConfig Config { get; }
        public IClientItemConfig ClientConfig { get; }
    }

    public interface IItemCatalog
    {
        UniTask<IReadOnlyList<ItemCatalogEntry>> LoadAsync();
    }

    public sealed class ItemCatalog : IItemCatalog
    {
        public ItemCatalog(IResourceLoader loader)
        {
            _loader = loader;
        }

        private readonly IResourceLoader _loader;
        private UniTask<IReadOnlyList<ItemCatalogEntry>>? _loading;

        public UniTask<IReadOnlyList<ItemCatalogEntry>> LoadAsync()
        {
            return _loading ??= LoadConfigsAsync().Preserve();
        }

        private async UniTask<IReadOnlyList<ItemCatalogEntry>> LoadConfigsAsync()
        {
            SharedItemDatabaseConfig shared = await _loader
                .LoadAsync<SharedItemDatabaseConfig>("SharedItemDatabase");
            ClientItemDatabaseConfig client = await _loader
                .LoadAsync<ClientItemDatabaseConfig>("ClientItemDatabase");

            var visuals = client.Items.ToDictionary(x => x.Identifier);
            return shared.Items.Select(x => new ItemCatalogEntry(x, visuals[x.Identifier])).ToArray();
        }
    }
}
