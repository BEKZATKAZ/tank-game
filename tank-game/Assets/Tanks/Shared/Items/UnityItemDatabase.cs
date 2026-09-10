using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Tanks.Core.Helpers;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Utils;
using Unity.Multiplayer;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Items
{
    public sealed class UnityItemDatabase : MonoBehaviour, IItemDatabase
    {
        [Required]
        [SerializeField]
        private SharedItemDatabaseConfig _database;

        private Dictionary<ItemIdentifier, IItemConfig> _items;
        private Dictionary<ItemIdentifier, ItemBehaviourBase[]> _behaviours;

        public IEnumerable<IItemConfig> Items => _items.Values;

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

        [Inject]
        public void Inject(IObjectResolver resolver)
        {
            MultiplayerRoleFlags role = MultiplayerRolesManager.ActiveMultiplayerRoleMask;
            bool isServer = role is MultiplayerRoleFlags.Server;

            _items = _database.Items
                .ToDictionary(k => k.Identifier, v => v as IItemConfig);

            _behaviours = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => IsValidBehaviourType(x, isServer))
                .GroupBy(x => x.GetCustomAttribute<ItemBehaviourAttribute>().TargetIdentifier)
                .ToDictionary(k => k.Key, v => v
                    .Distinct()
                    .Select(x =>
                    {
                        var behaviour = resolver.CreateInjected(x) as ItemBehaviourBase;
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

        private void OnDestroy()
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
    }
}
