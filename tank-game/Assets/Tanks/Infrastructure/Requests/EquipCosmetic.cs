using Newtonsoft.Json;

namespace Tanks.Infrastructure
{
    public struct EquipCosmetic
    {
        [JsonProperty("itemId")]
        public string ItemId;
    }
}
