using Cysharp.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tanks.Client.Items;
using Tanks.Core.Repositories;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.UI.Menus.Items;
using UnityEngine;

namespace Tanks.UI.Tests.Menus
{
    public sealed class ItemCatalogTests
    {
        [Test]
        public async Task Load_JoinsConfigsByIdAndCachesWithoutGameplayServices()
        {
            var shared = ScriptableObject.CreateInstance<SharedItemDatabaseConfig>();
            var client = ScriptableObject.CreateInstance<ClientItemDatabaseConfig>();

            try
            {
                var config = new ItemConfig();
                config.InitializeFromEditor("test", ItemTier.Epic,
                    new Dictionary<StatType, PlayerStat>(), new Dictionary<string, ItemStat>());
                shared.AddFromEditor(config);

                var visual = new ClientItemConfig();
                visual.InitializeFromEditor("test");
                visual.InitializeVisualsFromEditor(null, null, Color.magenta);
                client.AddFromEditor(visual);

                var loader = Substitute.For<IResourceLoader>();
                loader.LoadAsync<SharedItemDatabaseConfig>("SharedItemDatabase")
                    .Returns(UniTask.FromResult(shared));
                loader.LoadAsync<ClientItemDatabaseConfig>("ClientItemDatabase")
                    .Returns(UniTask.FromResult(client));

                var catalog = new ItemCatalog(loader);
                var first = await catalog.LoadAsync();
                var second = await catalog.LoadAsync();

                Assert.AreSame(first, second);
                Assert.AreSame(config, first[0].Config);
                Assert.AreSame(visual, first[0].ClientConfig);
                Assert.AreEqual(Color.magenta, first[0].ClientConfig.AccentColor);
                _ = loader.Received(1).LoadAsync<SharedItemDatabaseConfig>("SharedItemDatabase");
                _ = loader.Received(1).LoadAsync<ClientItemDatabaseConfig>("ClientItemDatabase");
            }
            finally
            {
                Object.DestroyImmediate(shared);
                Object.DestroyImmediate(client);
            }
        }
    }
}
