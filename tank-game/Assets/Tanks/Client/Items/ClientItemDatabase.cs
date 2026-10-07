using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Repositories;
using Tanks.Shared.Items;

namespace Tanks.Client.Items
{
    public sealed class ClientItemDatabase : IClientItemDatabase
    {
        public ClientItemDatabase(IResourceLoader loader)
        {
            _loader = loader;
        }

        private readonly IResourceLoader _loader;
        private Dictionary<ItemIdentifier, IClientItemConfig> _cached;

        public async UniTask WaitForInitializationAsync()
        {
            if (_cached != null) return;

            ClientItemDatabaseConfig database = await _loader
                .LoadAsync<ClientItemDatabaseConfig>("ClientItemDatabase");

            _cached = database.Items.ToDictionary(k => k.Identifier, v => v as IClientItemConfig);
        }

        public IClientItemConfig Get(ItemIdentifier id)
        {
            return _cached[id];
        }
    }
}
