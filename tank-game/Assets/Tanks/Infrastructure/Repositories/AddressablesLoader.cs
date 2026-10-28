using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core.Repositories;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tanks.Infrastructure.Repositories
{
    public sealed class AddressablesLoader : IResourceLoader
    {
        private readonly Dictionary<string, object> _cache = new();
        private readonly HashSet<string> _pending = new();

        private async Task<T> LoadInternalAsync<T>(
            string key, System.Func<Task<T>> func) where T : class
        {
            if (_cache.TryGetValue(key, out object cached)) return (T)cached;
            T result = await func();

            _cache[key] = result;
            return result;
        }

        private async Task<T> LoadAsync<T>(
            string key, System.Func<Task<T>> func) where T : class
        {
            if (_pending.Add(key) == false)
            {
                do await UniTask.Yield();
                while (_pending.Contains(key));
                return (T)_cache[key];
            }

            try
            {
                T result = await func();
                return result;
            }
            finally { _pending.Remove(key); }
        }

        public UniTask InitializeAsync()
        {
            return Addressables.InitializeAsync().ToUniTask();
        }

        public UniTask<T> LoadAsync<T>(AssetReferenceT<T> asset) where T : Object
        {
            string key = asset.AssetGUID;
            return LoadAsync(key, async () =>
                await LoadInternalAsync(key, () =>
                    asset.LoadAssetAsync().Task)).AsUniTask();
        }

        public UniTask<T> LoadAsync<T>(string key) where T : class
        {
            return LoadAsync(key, async () =>
                await LoadInternalAsync(key, () =>
                    Addressables.LoadAssetAsync<T>(key).Task)).AsUniTask();
        }

        public bool ReleaseFromKey(string key)
        {
            if (_cache.Remove(key, out object cached) == false) return false;
            Addressables.Release(cached);
            return true;
        }

        public bool ReleaseFromResource<T>(T resource) where T : class
        {
            string key = _cache.FirstOrDefault(x => resource.Equals(resource)).Key;
            if (key is null || _cache.Remove(key) == false) return false;

            Addressables.Release(resource);
            return true;
        }

        public void Dispose()
        {
            foreach (object cached in _cache.Values) Addressables.Release(cached);
            _cache.Clear();
            _pending.Clear();
        }
    }
}
