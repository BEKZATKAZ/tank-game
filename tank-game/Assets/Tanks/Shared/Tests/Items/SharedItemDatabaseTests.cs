using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Repositories;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Tests.Items
{
    public class SharedItemDatabaseTests
    {
        private IResourceLoader _loader;
        private IObjectResolver _container;
        private SharedItemDatabase _service;
        private SharedItemDatabaseConfig _databaseConfig;

        [SetUp]
        public void Setup()
        {
            _container = new ContainerBuilder().Build();
            _loader = Substitute.For<IResourceLoader>();
            _service = new SharedItemDatabase(_container, _loader);
            _databaseConfig = ScriptableObject.CreateInstance<SharedItemDatabaseConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_databaseConfig) Object.DestroyImmediate(_databaseConfig);
            _container.Dispose();
            _service.Dispose();
        }

        [Test]
        public void Initialize_DownloadsItems()
        {
            var tcs = new UniTaskCompletionSource<SharedItemDatabaseConfig>();

            _loader
                .LoadAsync<SharedItemDatabaseConfig>("SharedItemDatabase")
                .Returns(tcs.Task);

            _service.WaitForInitializationAsync();

            ItemConfig a = new ItemConfig();
            a.InitializeFromEditor("test-item1", ItemTier.Uncommon, new(), new());

            ItemConfig b = new ItemConfig();
            b.InitializeFromEditor("test-item2", ItemTier.Rare, new(), new());

            _databaseConfig.AddFromEditor(a);
            _databaseConfig.AddFromEditor(b);

            tcs.TrySetResult(_databaseConfig);

            Assert.That(_service.Get(new ItemIdentifier("test-item1")), Is.EqualTo(a));
            Assert.That(_service.Get(new ItemIdentifier("test-item2")), Is.EqualTo(b));

            // Should not call loader twice
            _service.WaitForInitializationAsync();
            _loader.Received(1).LoadAsync<SharedItemDatabaseConfig>(Arg.Any<string>());
        }
    }
}
