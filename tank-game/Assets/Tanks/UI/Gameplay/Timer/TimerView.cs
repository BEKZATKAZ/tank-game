using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Timer
{
    public sealed class TimerView : ViewBase<TimerViewModel>
    {
        private VisualElement _root;
        private Label _counter;
        private Label _label;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Timer");
            _counter = _root.Q<Label>("TimerCounter");
            _label = _root.Q<Label>("TimerText");
        }

        protected override void Bind(TimerViewModel viewModel)
        {
            viewModel.Seconds.Bind(x => _counter.text = x);
            viewModel.Label.Bind(x => _label.text = x);
            viewModel.CounterColor.Bind(x => _counter.style.color = x);
        }

        protected override void Update()
        {
            base.Update();
            ViewModel?.Tick(Time.deltaTime);
        }
    }
}
