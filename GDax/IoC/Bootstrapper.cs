using GDax.Commands;
using GDax.Enums;
using GDax.Views;
using GDax.Views.Models;
using StructureMap;
using System;

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

                foreach (CoinKind kind in Enum.GetValues(typeof(CoinKind)))
                {
                    a.For<IMenuItem>().Add<TickerItem>().Ctor<CoinKind>("kind").Is(kind).Named(kind.ToString());
                    a.For<ITickerViewModel>().Add<TickerViewModel>().Ctor<CoinKind>("kind").Is(kind).Named(kind.ToString());
                    a.For<TickerWidget>().Add<TickerWidget>().Ctor<ITickerViewModel>("model").Is(c => c.GetInstance<ITickerViewModel>(kind.ToString()));
//                    a.For<ITickerViewModel>().Singleton().Add<TickerViewModel>().Ctor<CoinKind>("kind").Is(kind).Named(kind.ToString());
                }
            });

            SettingsTypeGenerator.GenerateSettingTypes();
            return container;
        }
    }
}
