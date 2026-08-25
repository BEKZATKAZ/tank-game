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

        protected override VisualElement GetRootElement() => _label;

        protected override void Initialize(VisualElement root)
        {
            _label = root.Q<Label>("StartText");
            _label.style.display = DisplayStyle.Flex;
        }

        protected override void Bind(StartCountdownViewModel viewModel)
        {
            _instance ??= _audioFactory.CreateInstance(_countdownSoundPath);

            viewModel.Title.Bind(x =>
            {
                _label.text = x;
                if (string.IsNullOrEmpty(x) == false) _instance.Play();
            });
        }

        private void OnDestroy()
        {
            if (_instance is null) return;
            _instance.Stop(false);
            _instance.Dispose();
        }
    }
}
