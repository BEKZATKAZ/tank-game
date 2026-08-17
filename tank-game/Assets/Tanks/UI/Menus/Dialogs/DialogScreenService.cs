using Cysharp.Threading.Tasks;
using FMODUnity;
using System;
using UnityEngine;

namespace Tanks.UI.Menus.Dialogs
{
    public sealed class DialogScreenService : MonoBehaviour, IDialogScreenService
    {
        [SerializeField] private ViewBase<DialogScreenViewModel> _view;
        [SerializeField] private StudioEventEmitter _music;
        [SerializeField] private EventReference _errorSound;

        public void ShowConfirmation(string code, Action onYes = null, Action onNo = null)
        {
            _view.Show();
            _view.ViewModel.ShowConfirmation(code, onYes, onNo);
            MusicCutoffAsync().Forget();
        }

        public void ShowErrorFromCode(string prefix, string errorCode)
        {
            _view.Show();
            _view.ViewModel.ShowErrorFromCode(prefix, errorCode);

            RuntimeManager.PlayOneShot(_errorSound);
            if (_music) MusicCutoffAsync().Forget();
        }

        private async UniTaskVoid MusicCutoffAsync()
        {
            _music.SetParameter("MainMenuMusicCutoff", 1, ignoreseekspeed: false);

            while (_view.IsVisible)
            {
                if (Application.isPlaying == false) return;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            _music.SetParameter("MainMenuMusicCutoff", 0, ignoreseekspeed: false);
        }

        private void Start()
        {
            _view.gameObject.SetActive(true);
        }
    }
}
