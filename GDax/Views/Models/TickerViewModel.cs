using GDax.Helpers;
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
        double Percentage { get; }
        double Volume { get; set; }
    }

    public class TickerViewModel : ITickerViewModel, INotifyPropertyChanged
    {
        private double _price;
        private double _openPrice;
        private double _volume;

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
            if (!Product.Equals(product)) return;

            Price = data.Price;
            OpenPrice = data.OpenPrice;
            Volume = data.Volume24Hour;
        }

        public ITickerSetting Settings { get; }
        public IProduct Product { get; }
        public Brush NonActiveBackground { get; set; }
        public Brush Background => Brushes.White;
        public Brush Foreground => Brushes.Silver;
        public Brush PriceForeground { get; private set; }
        public Brush PercentageForeground { get; private set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public double Price
        {
            get { return _price; }
            set
            {
                if (_price == value)
                {
                    PriceForeground = Brushes.Black;
                }
                else if (_price < value)
                {
                    PriceForeground = Brushes.Green;
                }
                else
                {
                    PriceForeground = Brushes.Red;
                }
                _price = value;

                PercentageForeground = Percentage == 0 ? Brushes.Black : (Percentage < 0 ? Brushes.Red : Brushes.Green);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Price)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PriceForeground)));
            }
        }

        public double OpenPrice
        {
            get { return _openPrice; }
            set
            {
                _openPrice = value;
                PercentageForeground = Percentage == 0 ? Brushes.Black : (Percentage < 0 ? Brushes.Red : Brushes.Green);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Percentage)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PercentageForeground)));
            }
        }

        public double Percentage => (_price - _openPrice) / _openPrice;

        public double Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Volume)));
            }
        }

        public string TickerSymbol
        {
            get
            {
                if (Product is CurrencyPair)
                    return Utils.GetTicketSymbol(((CurrencyPair)Product).Base);

                return "";
            }
        }
    }
}