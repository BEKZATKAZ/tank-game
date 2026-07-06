using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Threading.Tasks;
using Tanks.Infrastructure.Repositories;
using UnityEngine;

namespace Tanks.Infrastructure.Tests.Repositories
{
    public class ResourceLoaderTests
    {
        private static readonly string _assetPath = "UI/DamagePreview";
        private AddressablesLoader _loader;

        [SetUp]
        public void Setup()
        {
            _loader = new AddressablesLoader();
        }

        [Test]
        public void Release_NoAsset_ReturnsFalse()
        {
            Assert.False(_loader.ReleaseFromKey(_assetPath), "Assets left from previous runs?");
            Assert.False(_loader.ReleaseFromResource(new object()), "Should not be cached");
        }

        [Test]
        public async Task LoadFromKey_ValidKey_CachesResult()
        {
            GameObject expected = await _loader.LoadAsync<GameObject>(_assetPath);
            Assert.NotNull(expected, "Should be a valid asset");

            GameObject cached = await _loader.LoadAsync<GameObject>(_assetPath);
            Assert.AreEqual(expected, cached, "Cached asset should be the same");

            Assert.True(_loader.ReleaseFromResource(cached), "Should successfuly release");
            Assert.False(_loader.ReleaseFromResource(cached), "Should already be released");

            GameObject newAsset = await _loader.LoadAsync<GameObject>(_assetPath);
            Assert.NotNull(newAsset, "New asset should be a valid asset");

            Assert.True(_loader.ReleaseFromKey(_assetPath), "Should successfuly release");
            Assert.False(_loader.ReleaseFromKey(_assetPath), "Should already be released");
        }

        [Test]
        public async Task LoadFromKey_Concurrent_DoesPending()
        {
            UniTask<GameObject>[] tasks = new UniTask<GameObject>[10];

            for (int i = 0; i < 10; i++)
            {
                tasks[i] = _loader.LoadAsync<GameObject>(_assetPath);
            }

            await UniTask.WhenAll(tasks);

            GameObject expected = tasks[0].GetAwaiter().GetResult();
            Assert.NotNull(expected, "Should be a valid asset");

            for (int i = 1; i < 10; i++)
            {
                GameObject actual = tasks[i].GetAwaiter().GetResult();
                Assert.AreEqual(expected, actual, "One key, different outcomes");
            }
        }

        [Test]
        public async Task Dispose_ReleasesAll()
        {
            await _loader.LoadAsync<GameObject>(_assetPath);
            _loader.Dispose();
        }

        [TearDown]
        public void TearDown()
        {
            _loader.Dispose();
        }
    }
}
