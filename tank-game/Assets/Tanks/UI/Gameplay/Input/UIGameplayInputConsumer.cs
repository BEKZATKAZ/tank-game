using NaughtyAttributes;
using Tanks.Client.Input;
using Tanks.Shared;
using Tanks.Shared.Bootstrap;
using Tanks.Shared.Messaging;
using Tanks.UI.Gameplay.GameStats;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Tanks.UI.Gameplay.Input
{
    public sealed class UIGameplayInputConsumer :
        MonoBehaviour,
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

        private bool _isGameEnded;

        private void LateUpdate()
        {
            UIGameplayInput input = _input.Read();

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

        private void Start()
        {
            _gameplayUI.rootVisualElement.style.display = DisplayStyle.None;
            _bus.RegisterHandler<GameStartedFromServer>(this);
            _bus.RegisterHandler<EndGameFromServer>(this);
        }

        private void OnDestroy()
        {
            _bus?.UnregisterHandler<GameStartedFromServer>(this);
            _bus?.UnregisterHandler<EndGameFromServer>(this);
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
