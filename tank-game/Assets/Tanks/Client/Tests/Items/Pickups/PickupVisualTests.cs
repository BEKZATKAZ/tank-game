using NSubstitute;
using NUnit.Framework;
using Tanks.Client.Items;
using Tanks.Client.Items.Pickups;
using UnityEngine;

namespace Tanks.Client.Tests.Items.Pickups
{
    public class PickupVisualTests
    {
        private GameObject _pickup;
        private PickupVisualLogic _logic;

        [SetUp]
        public void Setup()
        {
            _pickup = new GameObject("pickup");

            new PickupVisualSetup()
                .Setup(_pickup.transform, out var iconRenderer, out var glowRenderer);

            _logic = new PickupVisualLogic(iconRenderer, glowRenderer);
        }

        [TearDown]
        public void TearDown()
        {
            if (_pickup) Object.DestroyImmediate(_pickup);
        }

        [Test]
        public void Setup_ReturnsRenderers()
        {
            Assert.Greater(_logic.IconRenderer.sortingOrder, _logic.GlowRenderer.sortingOrder,
                "Icon sorting order should be greater than glow sorting order");

            Assert.AreEqual(_pickup.transform, _logic.IconRenderer.transform.parent,
                "Icon is not attached to pickup");

            Assert.AreEqual(_pickup.transform, _logic.GlowRenderer.transform.parent,
                "Glow is not attached to pickup");
        }

        [Test]
        public void Reset_AssignsVisuals()
        {
            Sprite icon = Sprite.Create(null, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            IClientItemConfig config = Substitute.For<IClientItemConfig>();

            config.AccentColor.Returns(Color.red);
            config.Icon.Returns(icon);
            config.FrameCount.Returns(1);

            _logic.Reset(config);

            Assert.AreEqual(icon, _logic.IconRenderer.sprite,
                "Icon sprite dismatch");

            Assert.AreEqual(1, _logic.IconBlock.GetInt("_FrameCount"),
                "Icon frame count dismatch");

            Assert.AreEqual(Color.red, _logic.GlowBlock.GetColor("_Color"),
                "Glow color dismatch");
        }
    }
}
