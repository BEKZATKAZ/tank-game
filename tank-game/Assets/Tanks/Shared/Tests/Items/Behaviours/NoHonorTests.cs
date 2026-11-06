using NSubstitute;
using NUnit.Framework;
using Tanks.Shared.Entities;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Behaviours.DamageBased;
using Tanks.Shared.Items.Data;
using Tanks.Shared.Weapons.Projectiles;
using Unity.Mathematics;

namespace Tanks.Shared.Tests.Items.Behaviours
{
    public sealed class NoHonorTests : ItemBehaviourTestBase<NoHonorBehaviour>
    {
        protected override void InitializeConfig() => AddItemStat("increase", new ItemStat { Value = 35 });

        [TestCase(0, 1.7f)]
        [TestCase(44, 1.7f)]
        [TestCase(46, 1)]
        [TestCase(90, 1)]
        [TestCase(180, 1)]
        public void DirectHit_RearConeUsesIncomingDirectionAndStackCount(float degrees, float expected)
        {
            ItemIdentifier id = Config.Identifier;
            MockOwner.Items.Returns(new[] { id, id });
            IEntityBehaviour target = Substitute.For<IEntityBehaviour>();
            target.Transform.Right.Returns(new float2(1, 0));
            float radians = math.radians(degrees);
            DirectHitContext context = new()
            {
                Target = target,
                Direction = new float2(math.cos(radians), math.sin(radians)),
                DamageMultiplier = 1
            };

            Behaviour.OnDirectHit(MockOwner, ref context);

            Assert.That(context.DamageMultiplier, Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
