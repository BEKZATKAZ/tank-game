using System;

namespace Tanks.Core.Lobbies.Sessions.DTOs
{
    public enum MapThickness
    {
        Thin,
        Medium,
        Thick
    }

    public enum MapSize
    {
        Small,
        Medium,
        Large
    }

    [Serializable]
    public struct MapGenerationSettingsFromLobby
    {
        public MapThickness Thickness;
        public MapSize Size;
    }
}
