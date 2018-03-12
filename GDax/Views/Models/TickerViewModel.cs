using GDax.Enums;
using GDax.IoC;
using GDax.Models;
using GDax.Settings;
using System.ComponentModel;
using System.Windows.Media;

namespace GDax.Views.Models
{
    public interface ITickerViewModel
    {
        ITickerSetting Settings { get; }
        CoinKind Kind { get; }
        Brush NonActiveBackground { get; }
        Brush Background { get; }
        Brush Foreground { get; }
        double Price { get; set; }
        double OpenPrice { get; set; }
        double Price24 { get; }
    }

    public class TickerViewModel : ITickerViewModel, INotifyPropertyChanged
    {
        private double _price;
        private double _openPrice;

        public TickerViewModel(ISettingsFactory factory, IFeed feed, CoinKind kind)
        {
            Kind = kind;
            Settings = factory.GetOrCreateSetting<ITickerSetting>(kind.ToString());
            NonActiveBackground = new SolidColorBrush(new Color { R = 255, G = 255, B = 255, A = 128 });

            feed.PriceUpdated += OnPriceUpdate;

            if (Settings.Subscribed)
                feed.Subscribe(Kind);
        }

        private void OnPriceUpdate(CoinKind kind, TickerResponse data)
        {
            if (kind != Kind) return;

            Price = data.Price;
            OpenPrice = data.OpenPrice;
        }

        public ITickerSetting Settings { get; }

        public CoinKind Kind { get; }

        public Brush NonActiveBackground { get; }

        public Brush Background => Brushes.White;

        public Brush Foreground => Brushes.Silver;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Price
        {
            get { return _price; }
            set
            {
                _price = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price24)));
            }
        }

        public double OpenPrice
        {
            get { return _openPrice; }
            set
            {
                _openPrice = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price24)));
            }
        }

        public double Price24 => (_price - _openPrice) / _openPrice;
    }
}
