using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GDax.Enums
{
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
}
