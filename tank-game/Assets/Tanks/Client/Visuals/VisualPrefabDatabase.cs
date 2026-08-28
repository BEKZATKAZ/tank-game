using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tanks.Client.Visuals
{
    [Serializable]
    public sealed class VisualPrefabData
    {
        [SerializeField]
        private VisualType _type;

        [Required]
        [SerializeField]
        private GameObject _prefab;

        [Range(1, 50)]
        [SerializeField]
        private int _maxInstanceCount = 10;

        public VisualType Type => _type;
        public GameObject Prefab => _prefab;
        public int MaxInstanceCount => _maxInstanceCount;
    }

    [CreateAssetMenu(fileName = "VisualDatabase", menuName = "Scriptable Objects/Visual Database")]
    public sealed class VisualPrefabDatabase : ScriptableObject
    {
        [SerializeField] private VisualPrefabData[] _visuals;

        public IEnumerable<VisualPrefabData> Visuals => _visuals;
    }
}
