using FMODUnity;
using Tanks.Localization;
using UnityEngine;

namespace Tanks.UI.Menus.Loading
{
    public sealed class LoadingScreenService : MonoBehaviour, ILoadingScreenService
    {
        [SerializeField] private ViewBase<LoadingViewModel> _view;
        [SerializeField] private StudioEventEmitter _music;

        public void Begin(ILocalizedString label)
        {
            _music.SetParameter("MainMenuMusicCutoff", 1, ignoreseekspeed: false);
            _view.Show();
            _view.ViewModel.Label.Value = label?.Get() ?? "";
        }

        public void End()
        {
            _music.SetParameter("MainMenuMusicCutoff", 0, ignoreseekspeed: false);
            _view.Hide();
        }

        private void Start()
        {
            _view.gameObject.SetActive(true);
        }
    }
}
