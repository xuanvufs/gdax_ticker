using GDax.Attributes;
using GDax.Converters;
using GDax.Enums;
using GDax.Helpers;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace GDax.Models
{
    [JsonConverter(typeof(ProductIdConverter))]
    public class CurrencyPair : IProduct
    {
        private string _baseSymbol;
        private string _targetSymbol;

        public CurrencyPair(Currency baseCurrency, Currency targetCurrency)
        {
            Base = baseCurrency;
            Target = targetCurrency;

            var _type = typeof(Currency);
            _baseSymbol = _type.GetMember(Base.ToString())[0].GetCustomAttribute<TickerSymbolAttribute>(false).TickerSymbol;
            _targetSymbol = _type.GetMember(Target.ToString())[0].GetCustomAttribute<TickerSymbolAttribute>(false).TickerSymbol;
        }

        public Currency Base { get; }

        public Currency Target { get; }

        public virtual string ProductId
        {
            get
            {
                return $"{_baseSymbol}-{_targetSymbol}";
            }
        }

        public static bool TryParse(string productId, out CurrencyPair currencyPair)
        {
            currencyPair = null;

            if (!string.IsNullOrWhiteSpace(productId))
            {
                var pair = productId.Split('-');

                if (pair.Length == 2)
                {
                    try
                    {
                        var b = Utils.FromTickerSymbol(pair[0]);
                        var t = Utils.FromTickerSymbol(pair[1]);

                        currencyPair = new CurrencyPair(b, t);

                        return true;
                    }
                    catch (Exception) { }
                }
            }

            return false;
        }

        public int CompareTo(IProduct other)
        {
            return string.Compare(ProductId, other.ProductId, true);
        }
    }
}