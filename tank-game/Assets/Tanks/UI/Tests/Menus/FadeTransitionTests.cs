using NSubstitute;
using NUnit.Framework;
using Tanks.Core.Common.Scenes;
using Tanks.UI.Menus.Transitions;

namespace Tanks.UI.Tests.Menus
{
    public class FadeTransitionTests : ViewModelTestBase<FadeTransitionViewModel>
    {
        private ISceneLoader _sceneLoader;

        protected override FadeTransitionViewModel CreateViewModel()
        {
            _sceneLoader = Substitute.For<ISceneLoader>();
            return new FadeTransitionViewModel(_sceneLoader);
        }

        [Test]
        public void EndMenuLoading_EndsMenuSceneLoading()
        {
            ViewModel.EndMenuLoading();
            _sceneLoader.Received(1).EndLoading<LoadMenus>();
        }
    }
}
