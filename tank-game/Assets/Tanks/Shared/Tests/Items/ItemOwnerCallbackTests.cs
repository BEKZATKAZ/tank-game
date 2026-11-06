using System;
using System.Reflection;
using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Messaging;
using UnityEngine;

namespace Tanks.Shared.Tests.Items
{
    public sealed class ItemOwnerCallbackTests
    {
        private sealed class CountingBehaviour : ItemBehaviourBase
        {
            public CountingBehaviour(bool once) => Once = once;
            public bool Once;
            public int Pickups;
            public int Ticks;
            public override bool OncePerOwner => Once;
            public override void OnPickup(IItemOwnerBehaviour owner) => Pickups++;
            public override void OnTick(IItemOwnerBehaviour owner, int tick, float tickDelta) => Ticks++;
        }

        [TestCase(true, 1)]
        [TestCase(false, 2)]
        public void RepeatedPickup_DeduplicatesOnlyOptedInCallbacks(bool once, int ticks)
        {
            ItemOwnerBehaviour owner = new GameObject().AddComponent<ItemOwnerBehaviour>();
            IItemConfig config = Substitute.For<IItemConfig>();
            IItemDatabase database = Substitute.For<IItemDatabase>();
            CountingBehaviour behaviour = new(once);
            config.Identifier.Returns(new ItemIdentifier("test-callback"));
            config.OnPickUpActions.Returns(Array.Empty<ItemActionConfigBase>());
            database.GetBehaviours(config.Identifier).Returns(new[] { behaviour });

            typeof(ItemOwnerBehaviour).GetField("_database", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(owner, database);
            typeof(ItemOwnerBehaviour).GetField("_bus", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(owner, Substitute.For<IMessageBus>());

            try
            {
                owner.PickupItem(config, default);
                owner.PickupItem(config, default);
                owner.OnTick(1, 1);

                Assert.That(behaviour.Pickups, Is.EqualTo(2));
                Assert.That(behaviour.Ticks, Is.EqualTo(ticks));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner.gameObject); }
        }
    }
}
