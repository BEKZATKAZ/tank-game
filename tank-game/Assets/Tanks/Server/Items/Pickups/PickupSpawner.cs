using System.Linq;
using System.Security.Cryptography;
using Tanks.Core.Lobbies.Sessions;
using Tanks.Shared;
using Tanks.Shared.Adapters;
using Tanks.Shared.Generation;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Items.Pickups;
using Tanks.Shared.Messaging;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Netcode;

namespace Tanks.Server.Items.Pickups
{
    public sealed class PickupSpawner :
        NetworkComponent<IPickupSpawnBehaviour>,
        IMessageHandler<TilemapGenerated>,
        IMessageHandler<GameStartedFromServer>,
        System.IDisposable
    {
        private const int MaxPlacementAttempts = 100;

        public PickupSpawner(
            IItemDatabase items,
            IMessageBus bus,
            ISessionSettingsService settings)
        {
            _items = items;
            _bus = bus;
            _settings = settings;

            _bus.RegisterHandler<TilemapGenerated>(this);
            _bus.RegisterHandler<GameStartedFromServer>(this);
        }

        public float CurrentCooldown { get; private set; }

        private readonly IItemDatabase _items;
        private readonly IMessageBus _bus;
        private readonly ISessionSettingsService _settings;
        private readonly NativeHashSet<int2> _occupiedCells = new(16, Allocator.Persistent);

        private ITransform[] _players;
        private ItemChooser _chooser;
        private Random _random;
        private TilemapGenerated? _lastTilemapGeneratedMessage;

        private float _elapsed;
        private bool _isGameStarted;

        public void Dispose()
        {
            _occupiedCells.Dispose();
        }

        public void ReleaseCell(float2 worldPosition)
        {
            if (_lastTilemapGeneratedMessage.HasValue == false) return;

            int wallWidth = _lastTilemapGeneratedMessage.Value.Settings.WallWidth;
            _occupiedCells.Remove(WorldToCell(worldPosition, wallWidth));
        }

        public void Handle(TilemapGenerated message, IMessageSender sender)
        {
            _lastTilemapGeneratedMessage = message;
            CurrentCooldown = 0;

            // Cells of the previous maze mean nothing in the new one
            _occupiedCells.Clear();
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
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

        protected override void OnInitialize()
        {
            ItemSpawnSettings settings = _settings.GetSettings()?.ItemSpawn ?? default;
            int seed = RandomNumberGenerator.GetInt32(int.MaxValue);

            _chooser = new ItemChooser(_items.Items, Behaviour.ItemCounts, seed);
            _random = Random.CreateFromIndex((uint)(seed ^ 23));

            CurrentCooldown = RollCooldown(settings);
        }

        protected override void OnDestroy()
        {
            _bus.UnregisterHandler<TilemapGenerated>(this);
            _bus.UnregisterHandler<GameStartedFromServer>(this);
        }

        protected override void OnTick(int tick, float tickDelta)
        {
            if (_isGameStarted == false) return;
            if (_lastTilemapGeneratedMessage.HasValue == false) return;

            _elapsed += tickDelta;
            if (_elapsed < CurrentCooldown) return;

            TilemapGenerated tilemap = _lastTilemapGeneratedMessage.Value;
            ItemSpawnSettings settings = _settings.GetSettings().ItemSpawn;

            if (TryFindFreeCell(tilemap, settings, out int2 cell) == false) return;

            CurrentCooldown = RollCooldown(settings);
            _elapsed = 0;

            SpawnPickup(cell, tilemap.Settings.WallWidth);
        }

        private static float2 CellToWorld(int2 cell, int wallWidth)
        {
            return (float2)cell * (wallWidth * 2) + wallWidth * 1.5f;
        }

        private static int2 WorldToCell(float2 worldPosition, int wallWidth)
        {
            return (int2)math.round((worldPosition - wallWidth * 1.5f) / (wallWidth * 2));
        }

        private float RollCooldown(ItemSpawnSettings settings)
        {
            return _random.NextFloat(settings.MinCooldownSeconds, settings.MaxCooldownSeconds);
        }

        private bool TryFindFreeCell(
            TilemapGenerated tilemap,
            ItemSpawnSettings settings,
            out int2 cell)
        {
            int wallWidth = tilemap.Settings.WallWidth;

            for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                cell = _random.NextInt2(0, tilemap.Resolution - 1);

                if (_occupiedCells.Contains(cell)) continue;

                // Prevent spawning right on players
                float distance = GetDistanceToNearestPlayer(cell, wallWidth);
                if (distance < settings.MinDistanceToPlayers) continue;

                return true;
            }

            cell = default;
            return false;
        }

        private float GetDistanceToNearestPlayer(int2 cell, int wallWidth)
        {
            float nearest = float.PositiveInfinity;

            foreach (ITransform player in _players)
            {
                if (player is not { IsValid: true }) continue;

                float2 playerPosition = player.WorldPosition;
                int2 playerCell = WorldToCell(playerPosition, wallWidth);

                nearest = math.min(nearest, math.distance(playerCell, cell));
            }

            return nearest;
        }

        private void SpawnPickup(int2 cell, int wallWidth)
        {
            IItemConfig item = _chooser.ChooseNext();
            _occupiedCells.Add(cell);

            Behaviour.SpawnEveryoneRpc(new PickupSpawned
            {
                ItemId = item.Identifier,
                WorldPosition = CellToWorld(cell, wallWidth)
            });
        }
    }
}
