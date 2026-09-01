using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Tanks.Core.Repositories;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Visuals
{
    public interface IVisualService
    {
        T Play<T>(VisualType type, float2 position = default, Angle rotation = default)
            where T : Component;
    }

    public sealed class VisualService : IVisualService
    {
        public VisualService(IResourceLoader loader)
        {
            LoadAsync(loader).Forget();
        }

        private Dictionary<VisualType, VisualBuffer> _buffers;

        public T Play<T>(VisualType type, float2 position = default, Angle rotation = default)
            where T : Component
        {
            VisualEntry entry = _buffers[type].Get();

            entry.Instance.SetActive(true);
            entry.Transform.WorldPosition = position;
            entry.Transform.WorldRotation = rotation;

            return entry.GetBehaviour<T>();
        }

        private async UniTaskVoid LoadAsync(IResourceLoader loader)
        {
            VisualPrefabDatabase database = await loader
                .LoadAsync<VisualPrefabDatabase>("Gameplay/Client/VisualDatabase");

            _buffers = database.Visuals.ToDictionary(
                k => k.Type,
                v => new VisualBuffer(v.Prefab, v.MaxInstanceCount));
        }
    }
}
