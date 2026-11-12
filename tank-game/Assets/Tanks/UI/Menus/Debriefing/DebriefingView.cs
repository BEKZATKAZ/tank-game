using Tanks.Core.Players;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI.Menus.Debriefing
{
    public sealed class DebriefingView : ViewBase<DebriefingViewModel>
    {
        private readonly DebriefingRowElement[] _rows = new DebriefingRowElement[5];
        private readonly DebriefingLevelBinding _levelBinding = new();

        [Inject] private readonly IPlayerLevelService _levels;

        [SerializeField] private string _statCountingSoundPath = "event:/UI/XPGain";
        [SerializeField] private string _levelUpSoundPath = "event:/UI/LevelUp";
        [Min(1)] [SerializeField] private float _maxPitch = 1.5f;

        private VisualElement _root;
        private VisualElement _content;
        private Label _error;
        private ProgressBar _progress;
        private Button _skip;
        private Button _close;
        private ScrollView _stats;
        private int _visibleRows;
        private int _boundFrame;
        private IVisualElementScheduledItem _scrollToRow;
        private IVisualElementScheduledItem _focusClose;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("DebriefingSection");
            _root.AddManipulator(new MenuHighlightManipulator());
            _content = _root.Q("DebriefingContent");
            _error = _root.Q<Label>("DebriefingError");
            _levelBinding.Bind(_root);
            _progress = _root.Q<ProgressBar>("DebriefingProgress");
            _stats = _root.Q<ScrollView>("DebriefingStats");
            _skip = _root.Q<Button>("DebriefingSkip");
            _close = _root.Q<Button>("DebriefingClose");
            _skip.clicked += () => ViewModel?.Skip();
            _close.clicked += CloseDebriefing;

            VisualElement rows = _root.Q("DebriefingRows");

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = new DebriefingRowElement();
                rows.Add(_rows[i]);
            }
        }

        protected override void Bind(DebriefingViewModel viewModel)
        {
            _visibleRows = 0;
            _boundFrame = Time.frameCount;
            viewModel.ConfigureAudio(
                _statCountingSoundPath, _levelUpSoundPath, _maxPitch);

            viewModel.Changed += Refresh;
            viewModel.Initialize();
            _focusClose = _close.schedule.Execute(() => _close.Focus());
        }

        protected override void Unbind(DebriefingViewModel viewModel)
        {
            _scrollToRow?.Pause();
            _focusClose?.Pause();
            viewModel.Changed -= Refresh;
        }

        protected override void Update()
        {
            base.Update();

            if (_boundFrame == Time.frameCount) return;

            ViewModel?.Tick(Time.unscaledDeltaTime);
        }

        private void Refresh()
        {
            DebriefingViewModel model = ViewModel;

            _error.style.display = model.IsFailure
                ? DisplayStyle.Flex : DisplayStyle.None;

            _content.style.display = model.IsFailure
                ? DisplayStyle.None : DisplayStyle.Flex;

            _skip.style.display = model.IsFailure
                ? DisplayStyle.None : DisplayStyle.Flex;

            _skip.SetEnabled(model.IsFailure == false &&
                model.Animation.IsComplete == false);

            if (model.IsFailure) return;

            for (int i = 0; i < _rows.Length; i++) _rows[i].Present(model, i);

            if (_visibleRows != model.Animation.VisibleRows &&
                model.Animation.VisibleRows > 0)
            {
                _visibleRows = model.Animation.VisibleRows;
                DebriefingRowElement row = _rows[_visibleRows - 1];

                _scrollToRow?.Pause();
                _scrollToRow = _stats.schedule.Execute(() => _stats.ScrollTo(row));
            }

            PlayerLevel level = model.Animation.Level;

            int required = _levels.RequiredXP(level.Value);

            _progress.value = model.Animation.Progress;
            _levelBinding.SetValue(level, required);
        }

        private void CloseDebriefing()
        {
            _scrollToRow?.Pause();
            _focusClose?.Pause();
            ViewModel?.Close();
        }
    }
}
