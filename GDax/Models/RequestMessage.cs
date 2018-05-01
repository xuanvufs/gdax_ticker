using GDax.Converters;
using GDax.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GDax.Models
{
    public class RequestMessage
    {
        [JsonProperty("type")]
        public RequestType Type { get; set; }

        [JsonProperty("product_ids")]
        public List<IProduct> Products { get; set; }

        [JsonProperty("channels")]
        [JsonConverter(typeof(ChannelListConverter))]
        public List<Channel> Channels { get; set; }
    }
}