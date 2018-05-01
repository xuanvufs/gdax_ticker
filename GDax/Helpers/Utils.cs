using GDax.Attributes;
using GDax.Enums;
using System;
using System.Linq;

namespace GDax.Helpers
{
    public static class Utils
    {
        public static Currency FromTickerSymbol(string tickerSymbol)
        {
            if (tickerSymbol == null) throw new ArgumentNullException("tickerSymbol");

            var type = typeof(Currency);
            var member = type.GetMembers().Where(c =>
            {
                var sym = c.GetCustomAttributes(typeof(TickerSymbolAttribute), false).FirstOrDefault() as TickerSymbolAttribute;
                return sym != null && string.Compare(tickerSymbol, sym.TickerSymbol, true) == 0;
            }).FirstOrDefault();

            if (member != null) return (Currency)Enum.Parse(type, member.Name);

            throw new Exception($"Currency not found for ticker symbol [{tickerSymbol}]");
        }
    }
}