using System.Collections.Generic;
using Tanks.Client.Entities;
using Tanks.Client.Rendering;
using Tanks.Shared.Entities;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Teams;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Gameplay.PlayerNames
{
    public sealed class PlayerNameEntry
    {
        public bool IsVisible { get; internal set; }
        public string DisplayName { get; internal set; }
        public Vector2 ScreenPosition { get; internal set; }
    }

    public sealed class PlayerNamesViewModel : IViewModel
    {
        public PlayerNamesViewModel(
            IEntityService entities,
            IClientEntityService clientEntity,
            ICameraMainService camera)
        {
            _entities = entities;
            _clientEntity = clientEntity;
            _camera = camera;
        }

        public IReadOnlyDictionary<IEntityBehaviour, PlayerNameEntry> Entries => _entries;
        public string SpectatorName { get; private set; } = "";

        private readonly IEntityService _entities;
        private readonly IClientEntityService _clientEntity;
        private readonly ICameraMainService _camera;
        private readonly Dictionary<IEntityBehaviour, PlayerNameEntry> _entries = new();
        private readonly HashSet<IEntityBehaviour> _seen = new();
        private readonly List<IEntityBehaviour> _removed = new();

        public void Tick()
        {
            _seen.Clear();
            ICamera camera = _camera.Get();
            IEntityBehaviour possessed = _clientEntity.GetPossessedEntity();
            SpectatorName = possessed is { IsValid: true, IsLocalPlayer: false }
                ? possessed.GetBehaviour<ITeamParticipantBehaviour>()?.State?.DisplayName ?? ""
                : "";

            foreach (IEntityBehaviour entity in _entities.GetAllEntities())
            {
                if (entity is not { IsValid: true } || entity == possessed) continue;
                if (entity.GetBehaviour<IHealthBehaviour>()?.ServerState.Value.Current is not > 0) continue;

                PlayerState state = entity.GetBehaviour<ITeamParticipantBehaviour>()?.State;
                if (state == null || state.IsEliminated) continue;

                _seen.Add(entity);
                if (_entries.TryGetValue(entity, out PlayerNameEntry entry) == false)
                {
                    entry = new PlayerNameEntry();
                    _entries.Add(entity, entry);
                }

                entry.IsVisible = false;
                if (camera == null) continue;

                float2 position = entity.Transform.WorldPosition;
                float2 viewport = camera.WorldToRenderedViewportPoint(position);
                if (IsOnScreen(viewport) == false) continue;

                position.y += entity.Size.y * 0.5f;
                viewport = camera.WorldToRenderedViewportPoint(position);
                if (IsOnScreen(viewport) == false) continue;

                Rect rect = camera.ViewportRect;
                entry.ScreenPosition = new Vector2(
                    rect.x + viewport.x * rect.width,
                    1 - (rect.y + viewport.y * rect.height));
                entry.DisplayName = state.DisplayName;
                entry.IsVisible = true;
            }

            _removed.Clear();
            foreach (IEntityBehaviour entity in _entries.Keys)
            {
                if (_seen.Contains(entity) == false) _removed.Add(entity);
            }

            foreach (IEntityBehaviour entity in _removed) _entries.Remove(entity);
        }

        private static bool IsOnScreen(float2 position)
        {
            return math.all(math.isfinite(position)) &&
                math.all(position >= 0) && math.all(position <= 1);
        }
    }
}
