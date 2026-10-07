using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tanks.Core.Helpers;
using Tanks.Core.Repositories;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Utils;
using Unity.Multiplayer;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Items
{
    public sealed class SharedItemDatabase : IItemDatabase
    {
        public SharedItemDatabase(IObjectResolver container, IResourceLoader loader)
        {
            _container = container;
            _loader = loader;
        }

        public IEnumerable<IItemConfig> Items => _items.Values;

        private readonly IObjectResolver _container;
        private readonly IResourceLoader _loader;

        private Dictionary<ItemIdentifier, IItemConfig> _items;
        private Dictionary<ItemIdentifier, ItemBehaviourBase[]> _behaviours;

        public void Dispose()
        {
            IEnumerable<IDisposable> disposables = _behaviours
                .SelectMany(x => x.Value)
                .OfType<IDisposable>();

            foreach (IDisposable behaviour in disposables)
            {
                try { behaviour.Dispose(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public async UniTask WaitForInitializationAsync()
        {
            if (_items != null) return;

            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            bool isServer = role is MultiplayerRoleFlags.Server;

            SharedItemDatabaseConfig database = await _loader
                .LoadAsync<SharedItemDatabaseConfig>("SharedItemDatabase");

            _items = database.Items
                .ToDictionary(k => k.Identifier, v => v as IItemConfig);

            _behaviours = UnityEngine.Assemblies.CurrentAssemblies
                .GetLoadedAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => IsValidBehaviourType(x, isServer))
                .GroupBy(x => x.GetCustomAttribute<ItemBehaviourAttribute>().TargetIdentifier)
                .ToDictionary(k => k.Key, v => v
                    .Distinct()
                    .Select(x =>
                    {
                        var behaviour = _container.CreateInjected(x) as ItemBehaviourBase;
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

        public IItemConfig Get(in ItemIdentifier id)
        {
            return _items[id];
        }

        private bool IsValidBehaviourType(Required<Type> type, bool isServer)
        {
            if (type.Value.IsAbstract) return false;

            ItemBehaviourAttribute attribute = type.Value
                .GetCustomAttribute<ItemBehaviourAttribute>();

            if (attribute is null) return false;

            // Discard invalid items
            if (_items.ContainsKey(attribute.TargetIdentifier) == false) return false;

            string assemblyName = type.Value.Assembly.FullName;
            string expectedToken = isServer ? "server" : "client";

            return
                assemblyName.Contains(expectedToken, StringComparison.InvariantCultureIgnoreCase) ||
                assemblyName.Contains("shared", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
