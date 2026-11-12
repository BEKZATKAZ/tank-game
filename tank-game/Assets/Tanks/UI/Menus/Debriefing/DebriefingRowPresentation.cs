using Tanks.Core.Debriefing;

namespace Tanks.UI.Menus.Debriefing
{
    public struct DebriefingRowPresentation
    {
        public bool IsVisible;
        public bool HasSpareParts;
        public bool HasPridePoints;
        public bool HasXP;
        public bool NegativeSpareParts;
        public bool NegativePridePoints;
        public string LabelKey;
        public StatDebriefing Value;
    }
}
