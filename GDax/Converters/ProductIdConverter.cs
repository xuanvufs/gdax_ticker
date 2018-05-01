using GDax.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace GDax.Converters
{
    public class ProductIdConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            if (CurrencyPair.TryParse(token.Value<string>(), out CurrencyPair pair))
            {
                return pair;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var product = value as IProduct;
            if (product == null) return;

            JToken.FromObject(product.ProductId).WriteTo(writer);
        }
    }
}