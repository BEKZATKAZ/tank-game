using NSubstitute;
using NUnit.Framework;
using Tanks.Server.Items.Behaviours.OnPickup;
using Tanks.Shared.Effects;
using Tanks.Shared.Entities.Health;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Tests.Items;
using Tanks.Utils;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Server.Tests.Items.Behaviours
{
    public sealed class NoclipTests : ItemBehaviourTestBase<NoclipBehaviour>
    {
        private IDamageService _damage;
        private EffectOwnerBehaviour _effects;
        private GameObject _wall;

        protected override void InitializeConfig() => AddItemStat("duration", new ItemStat { Value = 6 });

        protected override NoclipBehaviour CreateBehaviour()
        {
            _damage = Substitute.For<IDamageService>();
            return new NoclipBehaviour(_damage);
        }

        public override void Setup()
        {
            base.Setup();
            _effects = new GameObject().AddComponent<EffectOwnerBehaviour>();
            _wall = new GameObject("noclip-wall-test", typeof(BoxCollider2D));
            _wall.transform.position = new Vector3(5000, 5000, 0);
            Physics2D.SyncTransforms();

            MockOwner.IsServer.Returns(true);
            MockOwner.Entity.Size.Returns(new float2(1, 1));
            MockOwner.Entity.Transform.WorldScale.Returns(new float2(1, 1));
            MockOwner.Entity.Transform.WorldPosition.Returns(new float2(5000, 5000));
            MockOwner.Entity.GetBehaviour<IEffectOwnerBehaviour>().Returns(_effects);
            MockOwner.Entity.GetBehaviour<IHealthBehaviour>().ServerState.Returns(
                new BindableProperty<HealthState>(new HealthState(82, 100)));
        }

        [TearDown]
        public void TearDown()
        {
            Behaviour.OnDestroy(MockOwner);
            Object.DestroyImmediate(_effects.gameObject);
            Object.DestroyImmediate(_wall);
        }

        [Test]
        public void Expiry_InsideWallEliminatesIgnoringStats()
        {
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(6, 1);

            _damage.Received(1).Damage(82, MockOwner.Entity, includeStats: false);
        }

        [Test]
        public void Expiry_OutsideWallDoesNotDamage()
        {
            MockOwner.Entity.Transform.WorldPosition.Returns(new float2(5010, 5010));
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(6, 1);

            _damage.DidNotReceiveWithAnyArgs().Damage(default, default, default, default, default);
        }

        [Test]
        public void Expiry_InsideTriggerDoesNotEliminate()
        {
            _wall.GetComponent<BoxCollider2D>().isTrigger = true;
            Physics2D.SyncTransforms();
            Behaviour.OnPickup(MockOwner);
            _effects.OnTick(6, 1);

            _damage.DidNotReceiveWithAnyArgs().Damage(default, default, default, default, default);
        }
    }
}
