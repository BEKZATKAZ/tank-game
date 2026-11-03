using Tanks.UI.Menus.Dialogs;
using Tanks.UI.Menus.Sections;
using UnityEngine;

namespace Tanks.UI.Menus.Main
{
    public sealed class MainViewModel : IViewModel
    {
        public MainViewModel(IMenuSectionService sectionService, IDialogScreenService dialog)
        {
            _sectionService = sectionService;
            _dialog = dialog;
        }

        private readonly IMenuSectionService _sectionService;
        private readonly IDialogScreenService _dialog;

        public void OpenSection(MenuSectionType type)
        {
            _sectionService.OpenSection(type);
        }

        public void ShowQuitGameConfirmation()
        {
            _dialog.ShowConfirmation("quit", onYes: Quit);
        }

        private void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
