using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items;
using Tanks.Core.Repositories;
using Tanks.Shared.Items;
using UnityEngine;

namespace Tanks.Client.Tests.Items
{
    public class ClientItemDatabaseTests
    {
        private IResourceLoader _loader;
        private ClientItemDatabase _service;
        private ClientItemDatabaseConfig _databaseConfig;

        [SetUp]
        public void Setup()
        {
            _loader = Substitute.For<IResourceLoader>();
            _service = new ClientItemDatabase(_loader);
            _databaseConfig = ScriptableObject.CreateInstance<ClientItemDatabaseConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_databaseConfig) Object.DestroyImmediate(_databaseConfig);
        }

        [Test]
        public void Initialize_DownloadsItems()
        {
            var tcs = new UniTaskCompletionSource<ClientItemDatabaseConfig>();

            _loader
                .LoadAsync<ClientItemDatabaseConfig>("ClientItemDatabase")
                .Returns(tcs.Task);

            _service.WaitForInitializationAsync().Forget();

            ClientItemConfig a = new ClientItemConfig();
            a.InitializeFromEditor("test-item1");

            ClientItemConfig b = new ClientItemConfig();
            b.InitializeFromEditor("test-item2");

            _databaseConfig.AddFromEditor(a);
            _databaseConfig.AddFromEditor(b);

            tcs.TrySetResult(_databaseConfig);

            Assert.That(_service.Get(new ItemIdentifier("test-item1")), Is.EqualTo(a));
            Assert.That(_service.Get(new ItemIdentifier("test-item2")), Is.EqualTo(b));

            // Should not call loader twice
            _service.WaitForInitializationAsync().Forget();
            _loader.Received(1).LoadAsync<ClientItemDatabaseConfig>(Arg.Any<string>());
        }
    }
}
