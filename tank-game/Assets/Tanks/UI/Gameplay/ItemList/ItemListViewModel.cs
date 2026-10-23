using System;
using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Client.Items;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Messaging;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Gameplay.ItemList
{
    public sealed class ItemListEntry
    {
        public ItemListEntry(ItemIdentifier id, Sprite icon, int count)
        {
            Id = id;
            Icon = icon;
            Count = count;
        }

        public ItemIdentifier Id { get; }
        public Sprite Icon { get; }
        public int Count { get; }
    }

    public sealed class ItemListViewModel : IViewModel, IMessageHandler<ItemPickedUp>, IDisposable
    {
        public ItemListViewModel(
            IMessageBus bus,
            IClientItemDatabase clientItemDatabase,
            IClientEntityService clientEntityService)
        {
            _bus = bus;
            _clientItemDatabase = clientItemDatabase;
            _clientEntityService = clientEntityService;
            _bus.RegisterHandler<ItemPickedUp>(this);
            _clientEntityService.Changed += OnPossessed;
            OnPossessed(null, _clientEntityService.GetPossessedEntity());
        }

        public IReadOnlyBindableProperty<IReadOnlyList<ItemListEntry>> Entries => _entries;

        private readonly IMessageBus _bus;
        private readonly IClientItemDatabase _clientItemDatabase;
        private readonly IClientEntityService _clientEntityService;
        private readonly List<ItemListEntry> _items = new();
        private readonly Dictionary<ItemIdentifier, int> _indices = new();
        private readonly BindableProperty<IReadOnlyList<ItemListEntry>> _entries =
            new(Array.Empty<ItemListEntry>());

        public void Dispose()
        {
            _bus.UnregisterHandler<ItemPickedUp>(this);
            _clientEntityService.Changed -= OnPossessed;
        }

        public void Handle(ItemPickedUp message, IMessageSender sender)
        {
            if (message.Owner?.Entity is not { IsPossessed: true } entity) return;
            if (entity != _clientEntityService.GetPossessedEntity()) return;

            AddItem(message.ItemId);
            _entries.Value = _items.ToArray();
        }

        private void OnPossessed(IEntityBehaviour previous, IEntityBehaviour current)
        {
            _items.Clear();
            _indices.Clear();

            IItemOwnerBehaviour owner = current is { IsValid: true }
                ? current.GetBehaviour<IItemOwnerBehaviour>()
                : null;

            if (owner != null)
            {
                foreach (ItemIdentifier id in owner.Items) AddItem(id);
            }

            _entries.Value = _items.ToArray();
        }

        private void AddItem(ItemIdentifier id)
        {
            if (_indices.TryGetValue(id, out int index))
            {
                ItemListEntry entry = _items[index];
                _items[index] = new ItemListEntry(id, entry.Icon, entry.Count + 1);
                return;
            }

            _indices.Add(id, _items.Count);
            _items.Add(new ItemListEntry(id, _clientItemDatabase.Get(id).Icon, 1));
        }
    }
}
