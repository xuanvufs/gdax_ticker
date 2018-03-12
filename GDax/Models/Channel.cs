using GDax.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GDax.Models
{
    public class Channel
    {
        [JsonProperty("name")]
        public ChannelType Type { get; set; }

        [JsonProperty("product_ids")]
        public List<CoinKind> Products { get; set; }
    }
}
