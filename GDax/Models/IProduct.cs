using GDax.Converters;
using Newtonsoft.Json;
using System;

namespace GDax.Models
{
    [JsonConverter(typeof(ProductIdConverter))]
    public interface IProduct : IComparable<IProduct>
    {
        string ProductId { get; }
    }
}