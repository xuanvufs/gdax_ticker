using GDax.Enums;
using GDax.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace GDax.Converters
{
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
