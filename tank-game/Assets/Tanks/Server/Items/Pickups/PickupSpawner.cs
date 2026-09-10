using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Tanks.Core.Helpers;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Server.Items.Pickups
{
    public sealed class PickupSpawner :
        NetworkComponent<IPickupSpawnBehaviour>,
        IMessageHandler<TilemapGenerated>,
        IMessageHandler<GameStartedFromServer>,
        IMessageHandler<ItemPickedUp>
    {
        public PickupSpawner(IItemDatabase items, IMessageBus bus)
        {
            _items = items;
            _bus = bus;

            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<GameStartedFromServer>(this);
            _bus.RegisterHandler<ItemPickedUp>(this);
        }

        public float CurrentCooldown { get; private set; }

        private readonly IItemDatabase _items;
        private readonly IMessageBus _bus;

        private readonly Dictionary<PickupId, int2> _activePickups = new();
        private readonly HashSet<int2> _occupiedCoords = new();

        private ITransform[] _players;
        private ItemChooser _chooser;
        private Random _random;
        private PickupId _lastPickupId;
        private TilemapGenerated? _lastTilemapGeneratedMessage;

        private float _elapsed;
        private bool _isGameStarted;

        protected override void OnInitialize()
        {
            Dictionary<ItemTier, PositiveFloat> chanceRatios = new()
            {
                [ItemTier.Uncommon] = 50,
                [ItemTier.Rare] = 30,
                [ItemTier.Epic] = 12,
                [ItemTier.Unstable] = 6,
                [ItemTier.Legendary] = 2
            };

            uint seed = (uint)RandomNumberGenerator.GetInt32(0, int.MaxValue);
            _chooser = new ItemChooser(_items.Items, chanceRatios, seed);

            _random = Random.CreateFromIndex(seed ^ 23);
            CurrentCooldown = _random.NextFloat(Behaviour.MinCooldown, Behaviour.MaxCooldown);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _bus.UnregisterHandler<GameStartedFromServer>(this);
            _bus.UnregisterHandler<ItemPickedUp>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_isGameStarted == false) return;

            _elapsed += tickDelta;
            if (_elapsed < CurrentCooldown) return;

            _elapsed = 0;
            CurrentCooldown = _random.NextFloat(Behaviour.MinCooldown, Behaviour.MaxCooldown);
            _lastPickupId = new PickupId(_lastPickupId.Value + 1);

            IItemConfig item = _chooser.ChooseRandomItem(_random.NextFloat(0, 1f));
            TilemapGenerated message = _lastTilemapGeneratedMessage.Value;

            int wallWidth = message.Settings.WallWidth;
            int attempts = 0;
            int2 coords;

            while (true)
            {
                coords = _random.NextInt2(0, message.Resolution - 1);

                // Prevent infinite loops
                if (attempts++ > 100) return;

                // Search for a free spot
                if (_occupiedCoords.Contains(coords)) continue;

                float minDistance = float.PositiveInfinity;

                foreach (ITransform player in _players)
                {
                    if (player is not { IsValid: true }) continue;

                    float2 playerPosition = player.WorldPosition;

                    int2 playerCoords = (int2)math.round(
                        (playerPosition - wallWidth * 1.5f) / (wallWidth * 2));

                    float distance = math.distance(playerCoords, coords);
                    if (distance < minDistance) minDistance = distance;
                }

                // Prevent spawning right on players
                if (minDistance >= Behaviour.MinDistanceToPlayers) break;
            }

            float2 position = (float2)coords * (wallWidth * 2) + wallWidth * 1.5f;

            Behaviour.SpawnEveryoneRpc(new PickupSpawned
            {
                ItemId = item.Identifier,
                PickupId = _lastPickupId,
                WorldPosition = position
            });

            _activePickups.Add(_lastPickupId, coords);
            _occupiedCoords.Add(coords);
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _lastTilemapGeneratedMessage = message;
            CurrentCooldown = 0;
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            // Reset after the previous round
            _occupiedCoords.Clear();
            _activePickups.Clear();

            if (NetworkManager.Singleton)
            {
                _players = NetworkManager.Singleton.SpawnManager.PlayerObjects
                    .Select(x => new UnityTransform(x.transform))
                    .ToArray();
            }
            else _players = new ITransform[0];

            _elapsed = 0;
            _isGameStarted = true;
        }

        public void Handle(ItemPickedUp message, IMessageSender sender)
        {
            if (_activePickups.Remove(message.PickupId, out int2 coords) == false) return;
            _occupiedCoords.Remove(coords);
        }
    }
}
