using GDax.Attributes;
using GDax.Converters;
using GDax.Enums;
using GDax.Helpers;
using Newtonsoft.Json;
using System;
using System.Reflection;

namespace GDax.Models
{
    public class CurrencyPair : IProduct
    {
        private string _baseSymbol;
        private string _targetSymbol;

        public CurrencyPair(Currency baseCurrency, Currency targetCurrency)
        {
            if(baseCurrency == targetCurrency)
            {
                throw new ArgumentException($"Currency pair cannot have the same base and target currency. Base: [{baseCurrency}], Target: [{targetCurrency}]");
            }

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
                        var b = Utils.ParseTickerSymbol(pair[0]);
                        var t = Utils.ParseTickerSymbol(pair[1]);

                        currencyPair = new CurrencyPair(b, t);

                        return true;
                    }
                    catch (Exception) { }
                }
            }

            return false;
        }

        public bool Equals(IProduct other)
        {
            return ProductId.Equals(other.ProductId);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var product = obj as IProduct;
            if (product == null)
                return false;

            return Equals(obj);
        }

        public override int GetHashCode()
        {
            return ProductId.GetHashCode();
        }
    }
}