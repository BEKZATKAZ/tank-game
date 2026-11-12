using System;
using Tanks.Core.Debriefing;
using Tanks.Core.Players;

namespace Tanks.UI.Menus.Debriefing.Services
{
    public interface IDebriefingAnimationService
    {
        event Action<bool> CountingChanged;
        event Action LevelUp;
        StatDebriefing[] Rows { get; }
        PlayerLevel Level { get; }
        int VisibleRows { get; }
        bool IsComplete { get; }
        bool IsCounting { get; }
        float Progress { get; }
        void Initialize(PlayerDebriefing data);
        void Tick(float delta);
        void Skip();
        void Stop();
    }
}
