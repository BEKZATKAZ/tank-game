using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Extras
{
    public sealed class ExtrasView : ViewBase<ExtrasViewModel>
    {
        private VisualElement _root;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("ExtrasSection");
            _root.AddManipulator(new MenuHighlightManipulator());
            _root.Q<Button>("BackButton").clicked += () => ViewModel.Back();
            _root.Q<Button>("LeaderboardsButton").clicked += () => ViewModel.OpenLeaderboards();
        }
    }
}
