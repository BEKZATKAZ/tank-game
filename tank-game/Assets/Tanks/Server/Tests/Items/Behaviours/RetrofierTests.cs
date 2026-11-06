using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Stats;
using Tanks.Shared.Tests.Items;
using Tanks.Utils;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public sealed class RetrofierTests : ItemBehaviourTestBase<RetrofierBehaviour>
    {
        private IDamageService _damage;

        protected override void InitializeConfig()
        {
            AddItemStat("first", new ItemStat { Value = 100 });
            AddItemStat("second", new ItemStat { Value = 100 });
        }

        protected override RetrofierBehaviour CreateBehaviour()
        {
            _damage = Substitute.For<IDamageService>();
            return new RetrofierBehaviour(_damage);
        }

        [Test]
        public void Pickups_KeepFirstBonusThenKillIgnoringDefense()
        {
            MockOwner.Entity.GetBehaviour<IHealthBehaviour>().ServerState.Returns(
                new BindableProperty<HealthState>(new HealthState(100, 100)));
            IStatOwnerBehaviour stats = MockOwner.Entity.GetBehaviour<IStatOwnerBehaviour>();

            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id });
            Behaviour.OnPickup(MockOwner);
            MockOwner.Items.Returns(new[] { id, id });
            Behaviour.OnPickup(MockOwner);
            MockOwner.Items.Returns(new[] { id, id, id });
            Behaviour.OnPickup(MockOwner);

            stats.Received(1).AddStat(StatType.Defense, StatValueType.Flat, 100);
            stats.Received(1).AddStat(StatType.Damage, StatValueType.Flat, 100);
            stats.DidNotReceiveWithAnyArgs().RemoveStat(default);
            _damage.Received(1).Damage(100, MockOwner.Entity, includeStats: false);
        }
    }
}
