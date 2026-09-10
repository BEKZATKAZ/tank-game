using Tanks.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.Timer
{
    public sealed class TimerView : ViewBase<TimerViewModel>
    {
        private VisualElement _root;
        private Label _counter;

        protected override VisualElement GetRootElement() => _root;

        protected override void Initialize(VisualElement root)
        {
            _root = root.Q("Timer");
            _counter = _root.Q<Label>("TimerCounter");
        }

        protected override void Bind(TimerViewModel viewModel)
        {
            viewModel.Seconds.Bind(UpdateCounterText);
            viewModel.CounterColor.Bind(x => _counter.style.color = x);
        }

        protected override void Update()
        {
            base.Update();
            ViewModel?.Tick(Time.deltaTime);
        }

        private void UpdateCounterText(int number)
        {
            if (number <= 0) _counter.text = "--";
            else _counter.text = number.ToString("D2");
        }
    }
}
