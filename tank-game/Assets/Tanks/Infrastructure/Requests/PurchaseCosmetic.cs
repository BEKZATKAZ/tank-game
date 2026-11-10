using Newtonsoft.Json;

namespace Tanks.Infrastructure
{
    public struct PurchaseCosmetic
    {
        [JsonProperty("itemId")]
        public string ItemId;

        [JsonProperty("currency")]
        public string Currency;

        [JsonProperty("expectedPrice")]
        public int ExpectedPrice;
    }
}
