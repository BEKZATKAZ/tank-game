using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using Tanks.Shared.Adapters;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Visuals
{
    public sealed class UnityVisualService : MonoBehaviour, IVisualService
    {
        [Required]
        [SerializeField]
        private VisualPrefabDatabase _database;

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

        private void Awake()
        {
            _buffers = _database.Visuals.ToDictionary(
                k => k.Type,
                v => new VisualBuffer(v.Prefab, v.MaxInstanceCount));
        }
    }
}
