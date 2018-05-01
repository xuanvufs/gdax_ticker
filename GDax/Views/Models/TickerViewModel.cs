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
        IProduct Product { get; }
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

        public TickerViewModel(ISettingsFactory factory, IFeed feed, IProduct product)
        {
            Product = product;
            Settings = factory.GetOrCreateSetting<ITickerSetting>(product.ProductId);
            NonActiveBackground = new SolidColorBrush(new Color { R = 255, G = 255, B = 255, A = 128 });

            feed.PriceUpdated += OnPriceUpdate;

            if (Settings.Subscribed)
                feed.Subscribe(Product);
        }

        private void OnPriceUpdate(IProduct product, TickerResponse data)
        {
            if (Product.CompareTo(product) != 0) return;

            Price = data.Price;
            OpenPrice = data.OpenPrice;
        }

        public ITickerSetting Settings { get; }

        public IProduct Product { get; }

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