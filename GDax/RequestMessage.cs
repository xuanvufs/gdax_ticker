using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace GDax
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum RequestType
    {
        Subscribe,
        Unsubscribe
    }

    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum ResponseType
    {
        Subscriptions,
        Heartbeat,
        Ticker,
        Snapshot,
        L2update,
        Received,
        Open,
        Done,
        Match,
        Change,
        Activate,
        Error
    }

    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum ChannelType
    {
        Ticker,
        Heartbeat,
        Level2,
        User,
        Matches,
        Full
    }

    public class Channel
    {
        [JsonProperty("name")]
        public ChannelType Type { get; set; }

        [JsonProperty("product_ids")]
        public List<CoinKind> Products { get; set; }
    }

    public class RequestMessage
    {
        [JsonProperty("type")]
        public RequestType Type { get; set; }

        [JsonProperty("product_ids")]
        public List<CoinKind> Products { get; set; }

        [JsonProperty("channels")]
        [JsonConverter(typeof(ChannelListConverter))]
        public List<Channel> Channels { get; set; }
    }

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

    public class ChannelListConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var item = JArray.Load(reader);
            var channels = new List<Channel>();

            foreach (var child in item.Children())
            {
                if (child.Type == JTokenType.Object)
                    channels.Add(child.ToObject<Channel>());
                else channels.Add(new Channel { Type = child.Value<ChannelType>() });
            }

            return channels;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var channels = value as List<Channel>;
            if (channels == null || channels.Count == 0) return;

            writer.WriteStartArray();
            foreach (var channel in channels)
            {
                if (channel.Products != null && channel.Products.Count > 0)
                    JToken.FromObject(channel).WriteTo(writer);
                else JToken.FromObject(channel.Type).WriteTo(writer);
            }
            writer.WriteEndArray();
        }
    }
}
