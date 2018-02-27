using System.ComponentModel;
using System.Windows.Media;

namespace GDax
{
    public class CryptoCoin : INotifyPropertyChanged
    {

        public CryptoCoin(CoinKind coin)
        {
            Kind = coin;
            NonActiveBackground = new SolidColorBrush(new Color { R = 255, G = 255, B = 255, A = 128 });
        }

        private double _price;
        private double _openPrice;
        private bool _subscribed;

        public event PropertyChangedEventHandler PropertyChanged;

        public CoinKind Kind { get; }

        public Brush Background => Brushes.White;
        public Brush Foreground => Brushes.Silver;

        public Brush NonActiveBackground { get; set; }

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

        public bool Subscribed
        {
            get { return _subscribed; }
            set
            {
                _subscribed = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subscribed)));
            }
        }
    }
}
