using System;

namespace Tanks.Shared.Teams
{
    public enum PlayerPointType
    {
        Eliminations,
        Assistance,
        Deaths
    };

    public class PlayerState
    {
        public PlayerState(ulong playerId, string displayName, TeamId teamId)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            BelongsTo = teamId;
        }

        public event Action<PlayerState> Updated;

        public ulong PlayerId { get; }
        public string DisplayName { get; }
        public TeamId BelongsTo { get; }

        public bool IsEliminated { get; private set; }
        public bool IsLocalPlayer { get; private set; }

        public int Eliminations { get; private set; }
        public int Assistance { get; private set; }
        public int Deaths { get; private set; }

        public PlayerState SetEliminated(bool value)
        {
            IsEliminated = value;
            return this;
        }

        public PlayerState SetLocalPlayer(bool value)
        {
            IsLocalPlayer = value;
            return this;
        }

        public int GetPoints(PlayerPointType type) => type switch
        {
            PlayerPointType.Eliminations => Eliminations,
            PlayerPointType.Assistance => Assistance,
            PlayerPointType.Deaths => Deaths,
            _ => throw new NotImplementedException()
        };

        public void AddPoints(PlayerPointType type, int amount)
        {
            _ = type switch
            {
                PlayerPointType.Eliminations => Eliminations += amount,
                PlayerPointType.Assistance => Assistance += amount,
                PlayerPointType.Deaths => Deaths += amount,
                _ => throw new NotImplementedException()
            };

            Updated?.Invoke(this);
        }

        public void OverridePoints(int eliminations, int assistance, int deaths)
        {
            Eliminations = eliminations;
            Assistance = assistance;
            Deaths = deaths;
            Updated?.Invoke(this);
        }
    }
}
