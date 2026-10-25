using NaughtyAttributes;
using Tanks.Client.Input;
using Tanks.Client.Player;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.Shared.Playground;
using Tanks.UI.Gameplay.GameStats;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI.Gameplay.Input
{
    public sealed class UIGameplayInputConsumer :
        MonoBehaviour,
        IMessageHandler<PlaygroundStartedFromServer>,
        IMessageHandler<GameStartedFromServer>,
        IMessageHandler<EndGameFromServer>
    {
        [Required]
        [SerializeField]
        private UIDocument _gameplayUI;

        [Required]
        [SerializeField]
        private GameStatsView _gameStats;

        [Inject] private readonly IInputService<UIGameplayInput> _input;
        [Inject] private readonly IMessageBus _bus;
        [Inject] private readonly ISpectatorService _spectator;

        private bool _isGameEnded;
        private VisualElement _spectatorControls;
        private Button _previousTarget;
        private Button _nextTarget;

        private void LateUpdate()
        {
            UIGameplayInput input = _input.Read();
            UpdateSpectatorControls(input);

            bool gameStatsOpen = input.GameStatsOpen || _isGameEnded;

            if (gameStatsOpen && _gameStats.IsVisible == false)
            {
                _gameStats.Show();
            }

            else if (gameStatsOpen == false && _gameStats.IsVisible)
            {
                _gameStats.Hide();
            }
        }

        private void UpdateSpectatorControls(UIGameplayInput input)
        {
            if (input.PreviousSpectatorTarget) _spectator.PreviousTarget();
            else if (input.NextSpectatorTarget) _spectator.NextTarget();

            _spectatorControls.style.display = _spectator.IsSpectating
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            bool canSwitch = _spectator.CanSwitchTarget;
            _previousTarget.SetEnabled(canSwitch);
            _nextTarget.SetEnabled(canSwitch);
        }

        private void Start()
        {
            InitializeSpectatorControls(_gameplayUI.rootVisualElement);

            _gameplayUI.rootVisualElement.style.display = DisplayStyle.None;
            _bus.RegisterHandler<PlaygroundStartedFromServer>(this);
            _bus.RegisterHandler<GameStartedFromServer>(this);
            _bus.RegisterHandler<EndGameFromServer>(this);
        }

        private void InitializeSpectatorControls(VisualElement root)
        {
            _spectatorControls = root.Q("SpectatorControls");
            _previousTarget = root.Q<Button>("PreviousSpectatorTarget");
            _nextTarget = root.Q<Button>("NextSpectatorTarget");

            _previousTarget.clicked += _spectator.PreviousTarget;
            _nextTarget.clicked += _spectator.NextTarget;
        }

        private void OnDestroy()
        {
            if (_previousTarget != null) _previousTarget.clicked -= _spectator.PreviousTarget;
            if (_nextTarget != null) _nextTarget.clicked -= _spectator.NextTarget;

            _bus?.UnregisterHandler<PlaygroundStartedFromServer>(this);
            _bus?.UnregisterHandler<GameStartedFromServer>(this);
            _bus?.UnregisterHandler<EndGameFromServer>(this);
        }

        public void Handle(PlaygroundStartedFromServer message, IMessageSender sender)
        {
            _isGameEnded = false;
            _gameplayUI.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Handle(GameStartedFromServer message, IMessageSender sender)
        {
            _isGameEnded = false;
            _gameplayUI.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Handle(EndGameFromServer message, IMessageSender sender)
        {
            _isGameEnded = true;
        }
    }
}
