using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System;
using System.Collections;
using System.Threading.Tasks;
using Tanks.Infrastructure.Repositories;
using Tanks.Infrastructure.Tests.Runtime.Helpers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tanks.Infrastructure.Tests.Runtime
{
    public sealed class ResourceLoaderFailureTests
    {
        private AddressablesLoader _loader;
        private ControlledAssetProvider _provider;
        private ResourceLocationMap _locator;
        private AssetReferenceT<TextAsset> _reference;
        private TextAsset _asset;
        private Action<AsyncOperationHandle, Exception> _exceptionHandler;
        private string _key;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _loader = new AddressablesLoader();

            yield return _loader.InitializeAsync().ToCoroutine();

            _provider = new ControlledAssetProvider();
            _key = Guid.NewGuid().ToString("N");
            _reference = new AssetReferenceT<TextAsset>(_key);
            _locator = new ResourceLocationMap(_key);
            _asset = new TextAsset(_key);
            _exceptionHandler = ResourceManager.ExceptionHandler;

            _provider.Initialize(_key, null);

            _locator.Add(_key, new ResourceLocationBase(
                _key, _key, _provider.ProviderId, typeof(TextAsset)));

            Addressables.ResourceManager.ResourceProviders.Add(_provider);
            Addressables.AddResourceLocator(_locator);
            ResourceManager.ExceptionHandler = (_, _) => { };
        }

        [TearDown]
        public void TearDown()
        {
            _loader.Dispose();
            Addressables.RemoveResourceLocator(_locator);
            Addressables.ResourceManager.ResourceProviders.Remove(_provider);
            ResourceManager.ExceptionHandler = _exceptionHandler;
            Object.DestroyImmediate(_asset);
        }

        [UnityTest]
        public IEnumerator FailedKeyLoad_ReachesAllCallersAndCanRetry()
        {
            return FailedLoadAsync(false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator FailedReferenceLoad_ReachesAllCallersAndCanRetry()
        {
            return FailedLoadAsync(true).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator SuccessfulKeyLoad_IsSharedAndCached()
        {
            return SuccessfulLoadAsync(false).ToCoroutine();
        }

        [UnityTest]
        public IEnumerator SuccessfulReferenceLoad_IsSharedAndCached()
        {
            return SuccessfulLoadAsync(true).ToCoroutine();
        }

        private async UniTask FailedLoadAsync(bool useReference)
        {
            Exception expected = new InvalidOperationException("Bundle unavailable");
            Task<TextAsset> first = LoadAsync(useReference);
            Task<TextAsset> second = LoadAsync(useReference);

            await UniTask.WaitUntil(() => _provider.RequestCount > 0);

            _provider.Complete(null, expected);

            Exception firstError = await GetErrorAsync(first);
            Exception secondError = await GetErrorAsync(second);

            Assert.That(firstError, Is.Not.Null);
            Assert.That(secondError, Is.SameAs(firstError));
            Assert.That(firstError.ToString(), Does.Contain(expected.Message));
            Assert.That(_provider.RequestCount, Is.EqualTo(1));
            Assert.False(_loader.ReleaseFromKey(_key));

            Task<TextAsset> retry = LoadAsync(useReference);

            await UniTask.WaitUntil(() => _provider.RequestCount > 1);

            _provider.Complete(_asset);

            TextAsset result = await retry;

            Assert.That(result, Is.SameAs(_asset));
        }

        private async UniTask SuccessfulLoadAsync(bool useReference)
        {
            Task<TextAsset> first = LoadAsync(useReference);
            Task<TextAsset> second = LoadAsync(useReference);

            await UniTask.WaitUntil(() => _provider.RequestCount > 0);

            _provider.Complete(_asset);

            TextAsset firstResult = await first;
            TextAsset secondResult = await second;
            TextAsset cached = await LoadAsync(useReference);

            Assert.That(firstResult, Is.SameAs(_asset));
            Assert.That(secondResult, Is.SameAs(firstResult));
            Assert.That(cached, Is.SameAs(firstResult));
            Assert.That(_provider.RequestCount, Is.EqualTo(1));
        }

        private Task<TextAsset> LoadAsync(bool useReference)
        {
            return useReference
                ? _loader.LoadAsync(_reference).AsTask()
                : _loader.LoadAsync<TextAsset>(_key).AsTask();
        }

        private static async Task<Exception> GetErrorAsync(Task<TextAsset> task)
        {
            try { await task; }
            catch (Exception error) { return error; }

            return null;
        }
    }
}
