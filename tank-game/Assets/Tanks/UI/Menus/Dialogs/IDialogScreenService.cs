using System;

namespace Tanks.UI.Menus.Dialogs
{
    public interface IDialogScreenService
    {
        bool IsVisible { get; }

        void ShowConfirmation(string code, Action onYes = null, Action onNo = null);
        void ShowPurchaseConfirmation(string item, string currency, int price, Action onYes);
        void ShowErrorFromCode(string prefix, string errorCode);
    }
}
