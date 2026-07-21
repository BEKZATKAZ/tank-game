using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanks.Core.Repositories;
using Tanks.Infrastructure.Repositories;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Messaging;
using UnityEngine;
using VContainer;

namespace Tanks.Shared.Tests.Items
{
    public sealed class ItemDatabaseTests
    {
        private ItemDatabase _database;
        private IResourceLoader _loader;

        [SetUp]
        public void Setup()
        {
            _loader = new AddressablesLoader();

            ContainerBuilder builder = new ContainerBuilder();
            builder.Register<IMessageBus, MessageBus>(Lifetime.Scoped);

            _database = new ItemDatabase(_loader, builder.Build());
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
            _loader.Dispose();
        }

        [Test]
        public async Task Initialize_ShouldBeFine()
        {
            await _database.TryInitializeAsync(false);
            Assert.Pass();
        }

        [TestCase("health_up", ItemTier.Uncommon)]
        [TestCase("random_teleport", ItemTier.Rare)]
        public async Task Get_ReturnsTargetItem(string id, ItemTier tier)
        {
            await _database.TryInitializeAsync(false);
            IItemConfig item = _database.Get(new ItemIdentifier(id));

            Assert.AreEqual(id, item.Identifier.ToString());
            Assert.AreEqual(tier, item.Tier);
        }

        [Test]
        public async Task Get_InvalidId_Throws()
        {
            await _database.TryInitializeAsync(false);
            Assert.Throws<KeyNotFoundException>(() => _database.Get(new ItemIdentifier("invalid_id")));
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetBehaviours_ReturnsSplitBehaviours(bool isServer)
        {
            await _database.TryInitializeAsync(isServer);

            string unexpected = isServer ? "client" : "server";
            var behaviours = _database.GetBehaviours(new ItemIdentifier("health_up"));

            if (behaviours.Any() == false)
            {
                // ! The server must have at least one behaviour class
                if (isServer) Assert.Fail("No behaviours found at all");
                return;
            }

            bool invalid = behaviours
                .Any(x => x
                    .GetType().Assembly.FullName
                    .Contains(unexpected, System.StringComparison.InvariantCultureIgnoreCase));

            if (invalid == false) return;
            Assert.Fail("Server should not contain client types and vice versa");
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task GetAllBehaviours_ReturnsSplitBehaviours(bool isServer)
        {
            await _database.TryInitializeAsync(isServer);

            string unexpected = isServer ? "client" : "server";
            var allBehaviours = _database.GetAllBehaviours();

            if (allBehaviours.Any() == false)
            {
                // ! The server must have at least one behaviour class
                if (isServer) Assert.Fail("No behaviours found at all");
                return;
            }

            bool invalid = allBehaviours
                .SelectMany(x => x.Value)
                .Any(x => x
                    .GetType().Assembly.FullName
                    .Contains(unexpected, System.StringComparison.InvariantCultureIgnoreCase));

            if (invalid == false) return;
            Assert.Fail("Server should not contain client types and vice versa");
        }
    }
}
