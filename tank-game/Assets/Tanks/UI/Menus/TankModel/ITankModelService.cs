using System.Collections.Generic;
using Tanks.UI.Menus.TankModel;

namespace Tanks.UI.TankModel
{
    public interface ITankModelService
    {
        IReadOnlyList<TankModelEntry> Models { get; }
        bool IsDrivingInPlace { get; }

        void SetDisplayName(int tankIndex, string displayName);
        void SetReady(int tankIndex, bool ready);
        void StartDrivingInPlace();
        void DriveByCurve(int tankIndex, int curveIndex, bool instant);
        void StopDrivingInPlace();
        void Return(int index);
    }
}
