using Tanks.UI.Menus.Sections;
using UnityEngine.UIElements;

namespace Tanks.UI.Menus.Main
{
    public sealed class MainView : ViewBase<MainViewModel>
    {
        private VisualElement _root;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("MainSection");
            _root.AddManipulator(new MenuHighlightManipulator());
            _root.Q<Button>("QuitButton").clicked += OnQuitClicked;

            BindButton(_root.Q<Button>("PlayButton"), MenuSectionType.Play);
            BindButton(_root.Q<Button>("StoreButton"), MenuSectionType.Store);
            BindButton(_root.Q<Button>("SettingsButton"), MenuSectionType.Settings);
            BindButton(_root.Q<Button>("ExtrasButton"), MenuSectionType.Extras);
        }

        private void BindButton(Button button, MenuSectionType type)
        {
            button.clicked += () => ViewModel.OpenSection(type);
        }

        private void OnQuitClicked()
        {
            ViewModel.ShowQuitGameConfirmation();
        }
    }
}
