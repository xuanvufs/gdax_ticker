using GDax.Attributes;
using GDax.Enums;
using System;
using System.Linq;

namespace GDax.Helpers
{
    public static class Utils
    {
        public static Currency ParseCurrency(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException("value");

            if (Enum.TryParse(value, true, out Currency fromCurrencyName)) return fromCurrencyName;

            if (TryParseTickerSymbol(value, out Currency fromTickerSymbol)) return fromTickerSymbol;

            throw new Exception($"Currency not found for name [{value}]");
        }

        public static Currency ParseTickerSymbol(string value)
        {
            if (value == null) throw new ArgumentNullException("tickerSymbol");

            var type = typeof(Currency);
            var member = type.GetMembers().Where(c =>
            {
                var sym = c.GetCustomAttributes(typeof(TickerSymbolAttribute), false).FirstOrDefault() as TickerSymbolAttribute;
                return sym != null && string.Compare(value, sym.TickerSymbol, true) == 0;
            }).FirstOrDefault();

            if (member != null) return (Currency)Enum.Parse(type, member.Name, true);

            throw new Exception($"Currency not found for ticker symbol [{value}]");
        }

        public static bool TryParseTickerSymbol(string value, out Currency currency)
        {
            currency = (Currency)(-1);

            if (value == null) return false;

            try
            {
                currency = ParseTickerSymbol(value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}