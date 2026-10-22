using Tanks.Audio;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI.Gameplay.StartCountdown
{
    public sealed class StartCountdownView : ViewBase<StartCountdownViewModel>
    {
        [SerializeField] private string _countdownSoundPath = "event:/SFX/StartCounter";

        [Inject] private readonly IAudioFactory _audioFactory;

        private IAudioInstance _instance;
        private Label _label;
        private VisualElement _transitionOverlay;

        protected override VisualElement GetRootElement() => _label;

        protected override void Initialize(VisualElement root)
        {
            _label = root.Q<Label>("StartText");
            _label.style.display = DisplayStyle.Flex;
            _transitionOverlay = root.Q("TransitionOverlay");
        }

        protected override void Bind(StartCountdownViewModel viewModel)
        {
            _instance ??= _audioFactory.CreateInstance(_countdownSoundPath);

            viewModel.Title.Bind(x =>
            {
                _label.text = x;
                if (string.IsNullOrEmpty(x) == false) _instance.Play();
            });

            viewModel.TransitionOpacity.Bind(x => _transitionOverlay.style.opacity = x);
        }

        protected override void OnDestroy()
        {
            if (_instance is null) return;
            _instance.Stop(false);
            _instance.Dispose();
        }
    }
}
