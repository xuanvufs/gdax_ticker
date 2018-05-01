using System;

namespace GDax.Attributes
{
    public class TickerSymbolAttribute : Attribute
    {
        public string TickerSymbol { get; }

        public TickerSymbolAttribute(string tickerSymbol)
        {
            TickerSymbol = tickerSymbol;
        }
    }
}