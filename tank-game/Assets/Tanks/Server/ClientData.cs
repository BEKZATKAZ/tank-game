namespace Tanks.Server
{
    public sealed class ClientData
    {
        public ClientData(string displayName)
        {
            DisplayName = displayName;
        }

        public string DisplayName { get; }
    }
}
