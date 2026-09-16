using Tanks.Shared.Teams;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.GameStats
{
    [UxmlElement]
    public sealed partial class PlayerElement : VisualElement
    {
        public PlayerElement()
        {
            _displayName = new Label("Example Name") { name = "DisplayName" };
            _displayName.AddToClassList("game-stats__player-name");

            _eliminations = new Label("0") { name = "Eliminations" };
            _eliminations.AddToClassList("game-stats__player-eliminations");

            _assistance = new Label("0") { name = "Assistance" };
            _assistance.AddToClassList("game-stats__player-eliminations");

            _deaths = new Label("0") { name = "Deaths" };
            _deaths.AddToClassList("game-stats__player-deaths");

            _victories = new Label("0") { name = "Victories" };
            _victories.AddToClassList("game-stats__player-victories");

            Add(_displayName);
            Add(_eliminations);
            Add(_assistance);
            Add(_deaths);
            Add(_victories);

            AddToClassList("game-stats__player");
        }

        private readonly Label _displayName;
        private readonly Label _eliminations;
        private readonly Label _assistance;
        private readonly Label _deaths;
        private readonly Label _victories;

        public void Set(PlayerState state, Team team)
        {
            _eliminations.text = state.Eliminations.ToString();
            _assistance.text = state.Assistance.ToString();
            _deaths.text = state.Deaths.ToString();
            _victories.text = team.Score.Value.ToString();

            _displayName.text = state.DisplayName;
            if (state.IsLocalPlayer) _displayName.style.color = Color.yellow;
        }
    }
}
