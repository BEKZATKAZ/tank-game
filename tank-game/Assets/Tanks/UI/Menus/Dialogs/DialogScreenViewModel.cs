using System;
using Tanks.Localization;
using Tanks.Utils;
using UnityEngine;

namespace Tanks.UI.Menus.Dialogs
{
    public enum DialogType
    {
        Error,
        Confirmation
    }

    public sealed class DialogScreenViewModel : IViewModel
    {
        public DialogScreenViewModel(ILocalizationDatabase localization)
        {
            _localization = localization;
        }

        public BindableProperty<string> Header { get; } = new();
        public BindableProperty<string> Message { get; } = new();
        public BindableProperty<DialogType> Type { get; } = new();

        private readonly ILocalizationDatabase _localization;
        private Action _yes;
        private Action _no;

        public void ConfirmationYes()
        {
            try { _yes?.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }
            (_yes, _no) = (null, null);
        }

        public void ConfirmationNo()
        {
            try { _no?.Invoke(); }
            catch (Exception ex) { Debug.LogException(ex); }
            (_yes, _no) = (null, null);
        }

        public void ShowConfirmation(string code, Action onYes = null, Action onNo = null)
        {
            _yes = onYes;
            _no = onNo;

            string headerEntry = $"confirm.{code}.header";
            string messageEntry = $"confirm.{code}.message";

            _localization.TryGet("Confirmations", headerEntry, out ILocalizedString header);
            _localization.TryGet("Confirmations", messageEntry, out ILocalizedString message);

            Type.Value = DialogType.Confirmation;
            Header.Value = header?.Get() ?? "";
            Message.Value = message?.Get() ?? "";
        }

        public void ShowErrorFromCode(string prefix, string errorCode)
        {
            if (_localization.TryGet(
                "Errors", $"{prefix}.{errorCode}", out ILocalizedString text) == false)
            {
                Debug.LogWarningFormat("Invalid error entry '{0}'", $"{prefix}.{errorCode}");

                // Fallback to default
                _localization.TryGet("Errors", "error.default", out text);
            }

            Type.Value = DialogType.Error;
            Message.Value = text?.Get() ?? "";
        }
    }
}
