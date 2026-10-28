using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tanks.Core.Repositories
{
    public interface IResourceLoader : System.IDisposable
    {
        UniTask InitializeAsync();
        UniTask<T> LoadAsync<T>(AssetReferenceT<T> asset) where T : Object;
        UniTask<T> LoadAsync<T>(string key) where T : class;

        bool ReleaseFromKey(string key);
        bool ReleaseFromResource<T>(T resource) where T : class;
    }
}
