using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using Tanks.Client.Rendering;
using Tanks.UI.Menus.TankModel;
using Tanks.UI.TankModel;
using Unity.Mathematics;
using UnityEngine;

namespace Tanks.UI.Tests.Menus
{
    public sealed class MenuPlayerNamesTests : ViewModelTestBase<MenuPlayerNamesViewModel>
    {
        private GameObject _tank;
        private SpriteRenderer _sprite;
        private Sprite _tankSprite;
        private List<TankModelEntry> _models;
        private ITankModelService _tankModels;
        private ICameraMainService _cameraService;
        private ICamera _camera;

        protected override MenuPlayerNamesViewModel CreateViewModel()
        {
            _tank = new GameObject("Menu Tank");
            _tank.transform.position = new Vector3(0.4f, 0.3f);
            _sprite = _tank.AddComponent<SpriteRenderer>();
            _tankSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 10);
            _sprite.sprite = _tankSprite;
            _models = new List<TankModelEntry> { new(_tank.transform) };

            _tankModels = Substitute.For<ITankModelService>();
            _tankModels.Models.Returns(_models);
            _tankModels.When(x => x.SetDisplayName(Arg.Any<int>(), Arg.Any<string>())).Do(x =>
                typeof(TankModelEntry).GetProperty(nameof(TankModelEntry.DisplayName)).SetValue(_models[x.ArgAt<int>(0)], x.ArgAt<string>(1)));
            _tankModels.SetDisplayName(0, "Local");

            _camera = Substitute.For<ICamera>();
            _camera.ViewportRect.Returns(new Rect(0, 0, 1, 1));
            _camera.WorldToRenderedViewportPoint(Arg.Any<float2>()).Returns(x => (float2)x[0]);
            _cameraService = Substitute.For<ICameraMainService>();
            _cameraService.Get().Returns(_camera);

            return new MenuPlayerNamesViewModel(_tankModels, _cameraService);
        }

        [TearDown]
        public void DestroyTank()
        {
            Object.DestroyImmediate(_tank);
            Object.DestroyImmediate(_tankSprite);
        }

        [TestCase(-0.1f, 0.5f)]
        [TestCase(1.1f, 0.5f)]
        [TestCase(0.5f, -0.1f)]
        [TestCase(0.5f, 1.1f)]
        [TestCase(float.NaN, 0.5f)]
        public void Tick_HidesOffscreenOrInvalidProjection(float x, float y)
        {
            _camera.WorldToRenderedViewportPoint(Arg.Any<float2>()).Returns(new float2(x, y));
            ViewModel.Tick();

            Assert.IsFalse(ViewModel.Entries[0].IsVisible);
        }

        [Test]
        public void Tick_HidesMissingCameraDisabledSpritesAndEmptyNames()
        {
            _cameraService.Get().Returns((ICamera)null);
            ViewModel.Tick();
            Assert.IsFalse(ViewModel.Entries[0].IsVisible);

            _cameraService.Get().Returns(_camera);
            _sprite.enabled = false;
            ViewModel.Tick();
            Assert.IsFalse(ViewModel.Entries[0].IsVisible);

            _sprite.enabled = true;
            _tankModels.SetDisplayName(0, "");
            ViewModel.Tick();
            Assert.IsFalse(ViewModel.Entries[0].IsVisible);
        }
    }
}
