using GDax.Commands;
using GDax.Models;
using GDax.Settings;
using GDax.Views;
using GDax.Views.Models;
using StructureMap;
using System.Configuration;

namespace GDax.IoC
{
    public static class Bootstrapper
    {
        public static IContainer Init()
        {
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

                // Grab the Environments listed in the App.config and add them to our list.
                var connectionManagerDataSection = ConfigurationManager.GetSection(ProductConfigurationSection.SectionName) as ProductConfigurationSection;
                if (connectionManagerDataSection != null)
                {
                    foreach (CurrencyPairElement currencyPairEl in connectionManagerDataSection.CurrencyPairCollection)
                    {
                        var product = currencyPairEl.CurrencyPair;

                        a.For<IMenuItem>().Add<TickerItem>().Ctor<IProduct>("product").Is(product).Named(product.ProductId);
                        a.For<ITickerViewModel>().Add<TickerViewModel>().Ctor<IProduct>("product").Is(product).Named(product.ProductId);
                        a.For<TickerWidget>().Add<TickerWidget>().Ctor<ITickerViewModel>("model").Is(c => c.GetInstance<ITickerViewModel>(product.ProductId));
                    }
                }
            });

            SettingsTypeGenerator.GenerateSettingTypes();
            return container;
        }
    }
}