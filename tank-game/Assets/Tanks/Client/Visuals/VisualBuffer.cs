using System;
using Tanks.Shared.Adapters;
using UnityEngine;

namespace Tanks.Client.Visuals
{
    public sealed class VisualEntry
    {
        public VisualEntry(GameObject instance)
        {
            Instance = instance;
            Transform = new UnityTransform(instance.transform)
            {
                WorldPosition = -999
            };
        }

        public GameObject Instance { get; }
        public ITransform Transform { get; }
        private Component _behaviour;

        public T GetBehaviour<T>() where T : Component
        {
            if (_behaviour is T result) return result;
            _behaviour = Instance.GetComponent<T>();
            return _behaviour as T;
        }
    }

    public sealed class VisualBuffer : IDisposable
    {
        public VisualBuffer(GameObject prefab, int capacity)
        {
            _prefab = prefab;
            _buffer = new VisualEntry[capacity];
            Initialize();
        }

        private readonly VisualEntry[] _buffer;
        private readonly GameObject _prefab;
        private int _nextIndex;

        public VisualEntry Get()
        {
            VisualEntry entry = _buffer[_nextIndex++];
            if (_nextIndex >= _buffer.Length) _nextIndex = 0;
            return entry;
        }

        public void Dispose()
        {
            foreach (VisualEntry entry in _buffer)
            {
                if (entry.Instance) UnityEngine.Object.Destroy(entry.Instance);
            }
        }

        private void Initialize()
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                GameObject instance = UnityEngine.Object.Instantiate(_prefab);
                _buffer[i] = new VisualEntry(instance);
            }
        }
    }
}
