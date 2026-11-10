using System;
using System.Collections.Generic;

namespace Tanks.Core.Players
{
    public struct EquippedCosmetics
    {
        public string Body;
        public string Turret;

        public static EquippedCosmetics Default => new() { Body = "v3-body", Turret =
            "v3-turret" };
    }
}
