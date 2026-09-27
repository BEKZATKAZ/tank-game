using Tanks.Core.Common.Scenes;

namespace Tanks.UI.Menus.Transitions
{
    public sealed class FadeTransitionViewModel : IViewModel
    {
        public FadeTransitionViewModel(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader;
        }

        private readonly ISceneLoader _sceneLoader;

        public void EndMenuLoading()
        {
            _sceneLoader.EndLoading<LoadMenus>();
        }
    }
}
