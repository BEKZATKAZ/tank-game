using System.Collections.Generic;

namespace Tanks.Client.Input.Bindings
{
    public sealed class BindingDocument
    {
        public int Version { get; set; } = 1;
        public Dictionary<string, string> Overrides { get; set; } = new();
    }
}
