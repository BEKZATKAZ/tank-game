using Tanks.Shared.Teams;
using UnityEngine.UIElements;

namespace Tanks.UI.Gameplay.GameStats
{
    [UxmlElement]
    public sealed partial class PlayerElement : VisualElement
    {
        public PlayerElement()
        {
            _displayName = new Label("Example Name")
            {
                name = "DisplayName",
                enableRichText = true
            };

            _displayName.AddToClassList("game-stats__player-name");

            _eliminations = new Label("0") { name = "Eliminations" };
            _eliminations.AddToClassList("game-stats__player-eliminations");

            _assistance = new Label("0") { name = "Assistance" };
            _assistance.AddToClassList("game-stats__player-eliminations");

            _deaths = new Label("0") { name = "Deaths" };
            _deaths.AddToClassList("game-stats__player-deaths");

            _victories = new Label("0") { name = "Victories" };
            _victories.AddToClassList("game-stats__player-victories");

            _ping = new Label { name = "Ping" };
            _ping.AddToClassList("game-stats__player-ping");

            Add(_displayName);
            Add(_eliminations);
            Add(_assistance);
            Add(_deaths);
            Add(_victories);
            Add(_ping);

            AddToClassList("game-stats__player");
        }

        private readonly Label _displayName;
        private readonly Label _eliminations;
        private readonly Label _assistance;
        private readonly Label _deaths;
        private readonly Label _victories;
        private readonly Label _ping;

        public void SetPing(string text) => _ping.text = text;

        public void Set(PlayerState state, Team team, string displayName)
        {
            _eliminations.text = state.Eliminations.ToString();
            _assistance.text = state.Assistance.ToString();
            _deaths.text = state.Deaths.ToString();
            _victories.text = team.Score.Value.ToString();

            _displayName.text = displayName;

            _displayName.EnableInClassList(
                "game-stats__player-name--local", state.IsLocalPlayer);

            _displayName.EnableInClassList(
                "game-stats__player-name--eliminated", state.IsEliminated);
        }
    }
}
