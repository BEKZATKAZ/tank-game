using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using Tanks.Shared.Items;
using Tanks.Shared.Items.Pickups;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.Client.Tests.Items.Pickups
{
    public class PickupVisualTests
    {
        private PickupView _component;
        private IClientItemDatabase _database;

        [SetUp]
        public void Setup()
        {
            _database = Substitute.For<IClientItemDatabase>();
            _component = new PickupView(_database);
            _component.Initialize(Substitute.For<IPickupBehaviour>());
        }

        [TearDown]
        public void TearDown()
        {
            _component.Destroy();
        }

        [Test]
        public void Setup_ReturnsRenderers()
        {
            Assert.Greater(
                _component.IconRenderer.sortingOrder, _component.GlowRenderer.sortingOrder,
                "Icon sorting order should be greater than glow sorting order");
        }

        [Test]
        public void Update_SticksToRootTransform()
        {
            _component.Update(0.1f);
            _component.Transform.WorldPosition.Returns(new float2(100, 50));
            _component.Update(0.1f);

            Assert.AreEqual(new float2(100, 50), _component.IconTransform.WorldPosition);
            Assert.AreEqual(new float2(100, 50), _component.GlowTransform.WorldPosition);
        }

        [Test]
        public void Spawn_AssignsVisuals()
        {
            ItemIdentifier id = new ItemIdentifier("test");
            IClientItemConfig config = Substitute.For<IClientItemConfig>();

            Sprite icon = Sprite.Create(null, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

            Material iconMaterial = new Material(shader);
            Material glowMaterial = new Material(shader);
            
            config.IconMaterial.Returns(iconMaterial);
            config.GlowMaterial.Returns(glowMaterial);

            config.Icon.Returns(icon);
            config.AccentColor.Returns(Color.red);

            _database.Get(id).Returns(config);
            _component.Handle(new PickupSpawned { ItemId = id });

            Assert.AreEqual(
                icon, _component.IconRenderer.sprite,
                "Icon sprite dismatch");

            Assert.AreEqual(
                1, _component.IconBlock.GetInt("_FrameCount"),
                "Icon frame count dismatch");

            Assert.AreEqual(
                Color.red, _component.GlowBlock.GetColor("_Color"),
                "Glow color dismatch");
        }
    }
}
