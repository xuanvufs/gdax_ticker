using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GDax
{
    public class CoinToProductIdConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            var type = typeof(CoinKind);
            var coin = (from f in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                        let a = f.GetCustomAttribute<ProductIdAttribute>(false)
                        where a != null && a.ProductId == token.Value<string>()
                        select f.Name).FirstOrDefault();

            if (coin == null)
                return null;

            return Enum.Parse(type, coin);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = typeof(CoinKind);
            var name = Enum.GetName(type, value);
            var attr = type.GetMember(name).FirstOrDefault()?.GetCustomAttributes(typeof(ProductIdAttribute), false).FirstOrDefault() as ProductIdAttribute;

            if (attr == null) return;

            JToken.FromObject(attr.ProductId).WriteTo(writer);
        }
    }
}
