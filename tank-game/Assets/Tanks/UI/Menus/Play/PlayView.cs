using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Play
{
    public sealed class PlayView : ViewBase<PlayViewModel>
    {
        private VisualElement _root;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("PlaySection");
            _root.AddManipulator(new MenuHighlightManipulator());

            _root.Q<Button>("CreateLobbyButton").clicked += () => ViewModel.CreateLobbyAsync().Forget();
            _root.Q<Button>("FindLobbyButton").clicked += () => ViewModel.FindLobby();
            _root.Q<Button>("TutorialButton").clicked += () => ViewModel.OpenTutorialAsync().Forget();
            _root.Q<Button>("BackButton").clicked += () => ViewModel.Back();
        }
    }
}
