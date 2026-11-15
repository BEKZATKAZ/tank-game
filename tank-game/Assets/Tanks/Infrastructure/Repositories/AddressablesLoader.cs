using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core.Repositories;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Tanks.Infrastructure.Repositories
{
    public sealed class AddressablesLoader : IResourceLoader
    {
        private readonly Dictionary<string, object> _cache = new();
        private readonly Dictionary<string, Task> _pending = new();

        private async Task<T> LoadInternalAsync<T>(
            string key, Func<Task<T>> func) where T : class
        {
            try
            {
                T result = await func();
                _cache[key] = result;
                return result;
            }
            finally
            {
                _pending.Remove(key);
            }
        }

        private Task<T> LoadAsync<T>(string key, Func<Task<T>> func) where T : class
        {
            if (_cache.TryGetValue(key, out object cached))
            {
                return Task.FromResult((T)cached);
            }

            if (_pending.TryGetValue(key, out Task pending)) return (Task<T>)pending;
            Task<T> task = LoadInternalAsync(key, func);

            if (task.IsCompleted == false) _pending[key] = task;
            return task;
        }

        private static async Task<T> LoadAssetAsync<T>(AsyncOperationHandle<T> handle)
        {
            try
            {
                return await handle.Task;
            }
            catch
            {
                Addressables.Release(handle);
                throw;
            }
        }

        public UniTask InitializeAsync()
        {
            return Addressables.InitializeAsync().ToUniTask();
        }

        public UniTask<T> LoadAsync<T>(AssetReferenceT<T> asset) where T : Object
        {
            string key = asset.AssetGUID;
            return LoadAsync(key, () => LoadAssetAsync(asset.LoadAssetAsync()))
                .AsUniTask();
        }

        public UniTask<T> LoadAsync<T>(string key) where T : class
        {
            return LoadAsync(key, () => LoadAssetAsync(
                Addressables.LoadAssetAsync<T>(key))).AsUniTask();
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
