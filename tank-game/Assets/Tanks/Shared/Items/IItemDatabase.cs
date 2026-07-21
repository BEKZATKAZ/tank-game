using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tanks.Core.Helpers;
using Tanks.Core.Repositories;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using VContainer;

namespace Tanks.Shared.Items
{
    public interface IItemDatabase
    {
        bool IsReady { get; }
        IEnumerable<IItemConfig> Items { get; }

        UniTask TryInitializeAsync(bool isServer);
        IItemConfig Get(in ItemIdentifier id);

        IEnumerable<ItemBehaviourBase> GetBehaviours(in ItemIdentifier id);
        IEnumerable<KeyValuePair<ItemIdentifier, ItemBehaviourBase[]>> GetAllBehaviours();
    }

    public sealed class ItemDatabase : IItemDatabase, IDisposable
    {
        public ItemDatabase(IResourceLoader loader, IObjectResolver resolver)
        {
            _loader = loader;
            _resolver = resolver;
        }

        public bool IsReady => _items != null;
        public IEnumerable<IItemConfig> Items => _items.Values;

        private readonly IResourceLoader _loader;
        private readonly IObjectResolver _resolver;

        private Dictionary<ItemIdentifier, IItemConfig> _items;
        private Dictionary<ItemIdentifier, ItemBehaviourBase[]> _behaviours;

        private bool IsValidBehaviourType(Required<Type> type, bool isServer)
        {
            if (type.Value.IsAbstract) return false;
            if (type.Value.GetCustomAttribute<ItemBehaviourAttribute>() is null) return false;

            string assemblyName = type.Value.Assembly.FullName;
            string expectedToken = isServer ? "server" : "client";

            return
                assemblyName.Contains(expectedToken, StringComparison.InvariantCultureIgnoreCase) ||
                assemblyName.Contains("shared", StringComparison.InvariantCultureIgnoreCase);
        }

        public async UniTask TryInitializeAsync(bool isServer)
        {
            if (IsReady) return;

            SharedItemDatabaseConfig database = await _loader
                .LoadAsync<SharedItemDatabaseConfig>("Gameplay/ItemDatabase");

            _items = database.Items
                .ToDictionary(k => k.Identifier, v => v as IItemConfig);

            _behaviours = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => IsValidBehaviourType(x, isServer))
                .GroupBy(x => x.GetCustomAttribute<ItemBehaviourAttribute>().TargetIdentifier)
                .ToDictionary(k => k.Key, v => v
                    .Select(x =>
                    {
                        var behaviour = Activator.CreateInstance(x) as ItemBehaviourBase;
                        _resolver.Inject(behaviour);

                        IItemConfig targetConfig = _items[v.Key];
                        behaviour.Initialize(targetConfig);

                        return behaviour;
                    })
                    .ToArray());

        }

        public IEnumerable<ItemBehaviourBase> GetBehaviours(in ItemIdentifier id)
        {
            return _behaviours.TryGetValue(id, out ItemBehaviourBase[] result)
                ? result
                : Enumerable.Empty<ItemBehaviourBase>();
        }

        public IEnumerable<KeyValuePair<ItemIdentifier, ItemBehaviourBase[]>> GetAllBehaviours()
        {
            return _behaviours;
        }

        public IItemConfig Get(in ItemIdentifier id) => _items[id];
        public void Dispose() => _loader.ReleaseFromKey("Gameplay/ItemDatabase");
    }
}
