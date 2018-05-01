using GDax.Commands;
using GDax.Enums;
using GDax.Models;
using GDax.Views;
using GDax.Views.Models;
using StructureMap;
using System.Collections.Generic;

namespace GDax.IoC
{
    public static class Bootstrapper
    {
        public static IContainer Init()
        {
            var currencyPairs = new List<CurrencyPair>()
            {
                new CurrencyPair(Currency.BitCoin, Currency.Dollar),
                new CurrencyPair(Currency.Etherium, Currency.Dollar),
                new CurrencyPair(Currency.LiteCoin, Currency.Dollar),
                new CurrencyPair(Currency.BitCoinCash, Currency.Dollar)
            };

            var container = new Container(a =>
            {
                a.Scan(b =>
                {
                    b.TheCallingAssembly();
                    b.WithDefaultConventions();
                    b.AssembliesAndExecutablesFromApplicationBaseDirectory();
                });

                a.For<ISettingsFactory>().Singleton().Use<SettingsFactory>();
                a.For<IFeed>().Singleton().Use<Feed>();

                foreach (var product in currencyPairs)
                {
                    a.For<IMenuItem>().Add<TickerItem>().Ctor<IProduct>("product").Is(product).Named(product.ProductId);
                    a.For<ITickerViewModel>().Add<TickerViewModel>().Ctor<IProduct>("product").Is(product).Named(product.ProductId);
                    a.For<TickerWidget>().Add<TickerWidget>().Ctor<ITickerViewModel>("model").Is(c => c.GetInstance<ITickerViewModel>(product.ProductId));
                }
            });

            SettingsTypeGenerator.GenerateSettingTypes();
            return container;
        }
    }
}