using GDax.Converters;
using Newtonsoft.Json;
using System;

namespace GDax.Models
{
    [JsonConverter(typeof(ProductIdConverter))]
    public interface IProduct : IEquatable<IProduct>
    {
        string ProductId { get; }
    }
}