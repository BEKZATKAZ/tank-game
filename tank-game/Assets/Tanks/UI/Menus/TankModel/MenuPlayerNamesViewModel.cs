using System.Collections.Generic;
using Tanks.Client.Rendering;
using Tanks.UI.Gameplay.PlayerNames;
using Tanks.UI.TankModel;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Menus.TankModel
{
    public sealed class MenuPlayerNamesViewModel : IViewModel
    {
        private const float NameHeight = 1.1f;

        public MenuPlayerNamesViewModel(ITankModelService tankModels, ICameraMainService camera)
        {
            _tankModels = tankModels;
            _camera = camera;
        }

        public IReadOnlyList<PlayerNameEntry> Entries => _entries;

        private readonly ITankModelService _tankModels;
        private readonly ICameraMainService _camera;
        private readonly List<PlayerNameEntry> _entries = new();

        public int GetLevel(int index) => _tankModels.Models[index].Level;

        public bool IsReady(int index) => _tankModels.Models[index].IsReady;

        public Vector2 GetTankScreenPosition(int index)
        {
            Vector3 position = _tankModels.Models[index].Model.position;
            ICamera camera = _camera.Get();
            float2 viewport = camera.WorldToRenderedViewportPoint(new float2(position.x, position.y));
            Rect rect = camera.ViewportRect;

            return new Vector2(
                rect.x + viewport.x * rect.width,
                1 - (rect.y + viewport.y * rect.height));
        }

        public void Tick()
        {
            IReadOnlyList<TankModelEntry> models = _tankModels.Models;

            while (_entries.Count < models.Count)
            {
                _entries.Add(new PlayerNameEntry());
            }

            if (_entries.Count > models.Count)
            {
                _entries.RemoveRange(models.Count, _entries.Count - models.Count);
            }

            ICamera camera = _camera.Get();

            for (int i = 0; i < models.Count; i++)
            {
                TankModelEntry model = models[i];
                PlayerNameEntry entry = _entries[i];
                entry.IsVisible = false;

                if (camera == null ||
                    model.Model == false ||
                    model.Model.gameObject.activeInHierarchy == false) continue;

                if (string.IsNullOrEmpty(model.DisplayName)) continue;
                bool hasVisibleSprite = false;

                foreach (SpriteRenderer sprite in model.SpriteRenderers)
                {
                    if (sprite == false ||
                        sprite.enabled == false ||
                        sprite.gameObject.activeInHierarchy == false) continue;

                    hasVisibleSprite = true;
                    break;
                }

                if (hasVisibleSprite == false) continue;

                float2 position = new(model.Model.position.x, model.Model.position.y);
                if (IsOnScreen(camera.WorldToRenderedViewportPoint(position)) == false) continue;

                position.y += NameHeight;
                float2 viewport = camera.WorldToRenderedViewportPoint(position);

                if (IsOnScreen(viewport) == false) continue;
                Rect rect = camera.ViewportRect;

                entry.ScreenPosition = new Vector2(
                    rect.x + viewport.x * rect.width,
                    1 - (rect.y + viewport.y * rect.height));

                entry.DisplayName = model.DisplayName;
                entry.IsVisible = true;
            }
        }

        private static bool IsOnScreen(float2 position)
        {
            return math.all(math.isfinite(position)) &&
                math.all(position >= 0) && math.all(position <= 1);
        }
    }
}
