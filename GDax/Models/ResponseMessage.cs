using GDax.Converters;
using GDax.Enums;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace GDax.Models
{
    public class ResponseMessage
    {
        [JsonProperty("type")]
        public ResponseType Type { get; set; }
    }

    public class SubscriptionResponse : ResponseMessage
    {
        [JsonProperty("channels")]
        [JsonConverter(typeof(ChannelListConverter))]
        public List<Channel> Channels { get; set; }
    }

    public class TickerResponse : ResponseMessage
    {
        [JsonProperty("sequence")]
        public long Sequence { get; set; }

        [JsonProperty("product_id")]
        public CoinKind ProductId { get; set; }

        [JsonProperty("price")]
        public double Price { get; set; }

        [JsonProperty("open_24h")]
        public double OpenPrice { get; set; }

        [JsonProperty("volume_24h")]
        public double Volume24Hour { get; set; }

        [JsonProperty("low_24h")]
        public double Low24Hour { get; set; }

        [JsonProperty("high_24h")]
        public double High24Hour { get; set; }

        [JsonProperty("volume_30d")]
        public double Volume30Day { get; set; }

        [JsonProperty("best_bid")]
        public double BestBid { get; set; }

        [JsonProperty("best_ask")]
        public double BestAsk { get; set; }
    }
}
