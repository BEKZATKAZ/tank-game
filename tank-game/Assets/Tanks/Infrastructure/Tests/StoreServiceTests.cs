using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Core;
using Tanks.Core.Store;

namespace Tanks.Infrastructure.Tests
{
    public sealed class StoreServiceTests
    {
        [Test]
        public async Task Catalog_CoalescesRequestsAndReusesSuccessfulLoad()
        {
            IStoreBackendService backend = Substitute.For<IStoreBackendService>();
            TaskCompletionSource<ResultType<CosmeticDefinitions>> pending = new();
            backend.GetDefinitionsAsync().Returns(pending.Task);
            using StoreService cache = new(backend);

            Task<ResultType> first = cache.LoadCatalogAsync();
            Task<ResultType> second = cache.LoadCatalogAsync();
            Assert.That(second, Is.SameAs(first));
            pending.SetResult(Result.Success(new CosmeticDefinitions { Items = new() }));
            await first;
            await cache.LoadCatalogAsync();

            Assert.That(cache.IsCatalogLoaded, Is.True);
            _ = backend.Received(1).GetDefinitionsAsync();
        }

        [Test]
        public async Task Catalog_FailureCanRetryAndMismatchCanRefresh()
        {
            IStoreBackendService backend = Substitute.For<IStoreBackendService>();
            CosmeticDefinitions definitions = new() { Items = new Dictionary<string,
                CosmeticDefinition>() };

            backend.GetDefinitionsAsync().Returns(
                Result.Failure<CosmeticDefinitions>("unknown"),
                    Result.Success(definitions));

            using StoreService cache = new(backend);
            Assert.That((await cache.LoadCatalogAsync()).IsSuccess, Is.False);
            Assert.That((await cache.LoadCatalogAsync()).IsSuccess, Is.True);
            await cache.LoadCatalogAsync();
            _ = backend.Received(2).GetDefinitionsAsync();

            await cache.RefreshCatalogAsync();
            _ = backend.Received(3).GetDefinitionsAsync();
        }

        [Test]
        public async Task Catalog_DisposalReleasesDataAndRejectsLateCompletion()
        {
            IStoreBackendService backend = Substitute.For<IStoreBackendService>();
            TaskCompletionSource<ResultType<CosmeticDefinitions>> pending = new();
            backend.GetDefinitionsAsync().Returns(pending.Task);
            StoreService cache = new(backend);
            Task<ResultType> loading = cache.LoadCatalogAsync();
            cache.Dispose();
            pending.SetResult(Result.Success(new CosmeticDefinitions { Items = new() }));

            Assert.That((await loading).IsSuccess, Is.False);
            Assert.That(cache.Definitions.Items, Is.Null);
            Assert.That(cache.IsCatalogLoaded, Is.False);
            Assert.That((await cache.LoadCatalogAsync()).IsSuccess, Is.False);
            _ = backend.Received(1).GetDefinitionsAsync();
        }

        [Test]
        public async Task Catalog_NewMenusSceneStartsWithAnEmptyCache()
        {
            IStoreBackendService backend = Substitute.For<IStoreBackendService>();
            backend.GetDefinitionsAsync().Returns(Result.Success(new CosmeticDefinitions
                { Items = new() }));

            StoreService firstScene = new(backend);
            await firstScene.LoadCatalogAsync();
            firstScene.Dispose();
            Assert.That(firstScene.Definitions.Items, Is.Null);

            using StoreService nextScene = new(backend);
            await nextScene.LoadCatalogAsync();
            _ = backend.Received(2).GetDefinitionsAsync();
        }
    }
}
