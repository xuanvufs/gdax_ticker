using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GDax.Enums
{
    [JsonConverter(typeof(StringEnumConverter), true)]
    public enum RequestType
    {
        Subscribe,
        Unsubscribe
    }
}
