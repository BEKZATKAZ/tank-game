using System;

namespace Tanks.UI.Menus.Dialogs
{
    public interface IDialogScreenService
    {
        bool IsVisible { get; }

        void ShowConfirmation(string code, Action onYes = null, Action onNo = null);
        void ShowErrorFromCode(string prefix, string errorCode);
    }
}
