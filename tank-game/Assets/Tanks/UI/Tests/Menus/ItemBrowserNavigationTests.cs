using NUnit.Framework;
using System;
using System.Reflection;
using Tanks.UI.Menus.Items;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Tanks.UI.Tests.Menus
{
    public sealed class ItemBrowserNavigationTests
    {
        private GameObject _documentObject;
        private PanelSettings _settings;
        private RenderTexture _texture;
        private VisualElement _section;
        private VisualElement _grid;
        private ScrollView _scroll;
        private Button _back;

        [SetUp]
        public void Setup()
        {
            _texture = new RenderTexture(64, 64, 0);
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.targetTexture = _texture;
            _documentObject = new GameObject("Navigation Test");
            var document = _documentObject.AddComponent<UIDocument>();
            document.panelSettings = _settings;

            _section = new VisualElement();
            _back = new Button();
            _scroll = new ScrollView { focusable = false };
            _grid = new VisualElement();
            _scroll.Add(_grid);
            _section.Add(_back);
            _section.Add(_scroll);
            document.rootVisualElement.Add(_section);
            AddItems(15);

            Type type = typeof(ItemBrowserView).Assembly.GetType(
                "Tanks.UI.Menus.Items.ItemBrowserNavigationManipulator", true);
            var manipulator = (IManipulator)Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.Public, null, new object[] { _grid, _back }, null);
            _section.AddManipulator(manipulator);

            Assert.IsNotNull(_section.panel);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_documentObject);
            Object.DestroyImmediate(_settings);
            Object.DestroyImmediate(_texture);
        }

        private void AddItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = new VisualElement { focusable = true, userData = i };
                item.Add(new VisualElement());
                _grid.Add(item);
            }
        }

        private static void Move(VisualElement origin, NavigationMoveEvent.Direction direction)
        {
            using var evt = NavigationMoveEvent.GetPooled(direction);
            evt.target = origin;
            origin.SendEvent(evt);
        }

        [TestCase(6)]
        [TestCase(13)]
        [TestCase(14)]
        public void Right_FromRowEnd_FocusesBackBeforeScrollViewHandlesEvent(int index)
        {
            bool reachedScroll = false;
            _scroll.RegisterCallback<NavigationMoveEvent>(_ => reachedScroll = true, TrickleDown.TrickleDown);
            _grid[index].Focus();

            Move(_grid[index], NavigationMoveEvent.Direction.Right);

            Assert.AreSame(_back, _section.focusController.focusedElement);
            Assert.IsFalse(reachedScroll);
        }

        [Test]
        public void ItemBox_ImplementsExistingHighlightContract()
        {
            Type contract = typeof(ItemBrowserView).Assembly.GetType("Tanks.UI.Menus.IMenuHighlightTarget", true);
            Assert.IsTrue(contract.IsAssignableFrom(typeof(ItemBox)));
        }

        [TestCase(0)]
        [TestCase(7)]
        [TestCase(14)]
        public void Left_FromRowStart_FocusesBackAndRestoresItem(int index)
        {
            _grid[index].Focus();
            Move(_grid[index], NavigationMoveEvent.Direction.Left);
            Assert.AreSame(_back, _section.focusController.focusedElement);

            Move(_back, NavigationMoveEvent.Direction.Left);
            Assert.AreSame(_grid[index], _section.focusController.focusedElement);
        }

        [Test]
        public void Left_FromBackWithoutHistory_FocusesFirstItem()
        {
            _back.Focus();

            Move(_back, NavigationMoveEvent.Direction.Left);

            Assert.AreSame(_grid[0], _section.focusController.focusedElement);
        }

        [Test]
        public void Left_FromBack_RestoresLastFocusedItem()
        {
            _grid[8].Focus();
            _back.Focus();

            Move(_back, NavigationMoveEvent.Direction.Left);

            Assert.AreSame(_grid[8], _section.focusController.focusedElement);
        }

        [Test]
        public void Left_AfterGridRebuild_RestoresItemByIdentifier()
        {
            _grid[8].Focus();
            _back.Focus();
            _grid.Clear();
            AddItems(15);

            Move(_back, NavigationMoveEvent.Direction.Left);

            Assert.AreSame(_grid[8], _section.focusController.focusedElement);
        }

        [Test]
        public void NestedTarget_RightFromRowEnd_FocusesBack()
        {
            _grid[6].Focus();

            Move(_grid[6][0], NavigationMoveEvent.Direction.Right);

            Assert.AreSame(_back, _section.focusController.focusedElement);
        }

        [TestCase(NavigationMoveEvent.Direction.Up)]
        [TestCase(NavigationMoveEvent.Direction.Down)]
        [TestCase(NavigationMoveEvent.Direction.Left)]
        [TestCase(NavigationMoveEvent.Direction.Right)]
        public void InteriorGridMovement_ContinuesThroughNativeEventRoute(NavigationMoveEvent.Direction direction)
        {
            bool reachedScroll = false;
            _scroll.RegisterCallback<NavigationMoveEvent>(_ => reachedScroll = true, TrickleDown.TrickleDown);
            _grid[8].Focus();

            Move(_grid[8], direction);

            Assert.IsTrue(reachedScroll);
        }

        [Test]
        public void Left_FromBackWithEmptyGrid_IsNotConsumed()
        {
            _grid.Clear();
            bool reachedBack = false;
            _back.RegisterCallback<NavigationMoveEvent>(_ => reachedBack = true);
            _back.Focus();

            Move(_back, NavigationMoveEvent.Direction.Left);

            Assert.IsTrue(reachedBack);
        }
    }
}
