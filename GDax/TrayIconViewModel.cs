using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GDax
{
    public interface ICommandItem
    {
        string Text { get; }
        string ToolTip { get; }
        ICommand Command { get; }
        object Parameter { get; }
        bool Checkable { get; }
        bool Checked { get; }
    }

    public class SimpleCommandItem : ICommandItem
    {
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public ICommand Command { get; set; }
        public object Parameter { get; set; }
        public bool Checkable { get; set; }
        public bool Checked { get; set; }
    }

    public class TickerWidgetCommandItem : ICommandItem
    {
        private CryptoCoin _coin;

        public TickerWidgetCommandItem(CryptoCoin coin)
        {
            _coin = coin;
        }

        public CryptoCoin Coin => _coin;

        public string Text => _coin.Kind.ToString();

        public string ToolTip => _coin.Kind.ToString();

        public ICommand Command { get; set; }

        public object Parameter => this;

        public bool Checked
        {
            get
            {
                return _coin.Subscribed;
            }
            set
            {
                _coin.Subscribed = value;
            }
        }

        public bool Checkable => true;
    }

    public class TrayIconViewModel : IDisposable
    {
        private Feed _feed;
        private Dictionary<CoinKind, CryptoCoin> _coins = new Dictionary<CoinKind, CryptoCoin>();

        public TrayIconViewModel()
        {
            var kinds = Enum.GetValues(typeof(CoinKind));
            var items = new List<ICommandItem>();
            foreach (CoinKind kind in kinds)
            {
                var coin = new CryptoCoin(kind);

                _coins.Add(kind, coin);
                items.Add(new TickerWidgetCommandItem(coin)
                          {
                              Command = new DelegateCommand(ToggleTicker)
                          });
            }

            items.Add(null);
            items.Add(new SimpleCommandItem { Text = "Exit", Command = new DelegateCommand((obj) => Application.Current.Shutdown()) });

            MenuItems = items.AsReadOnly();
            Coins = _coins.Values.ToList().AsReadOnly();

            _feed = new Feed();
            _feed.PriceUpdated += OnPriceUpdated;
        }

        public IReadOnlyCollection<ICommandItem> MenuItems { get; }

        public IReadOnlyCollection<CryptoCoin> Coins { get; }

        void OnPriceUpdated(CoinKind coin, TickerResponse data)
        {
            _coins[coin].Price = data.Price;
            _coins[coin].OpenPrice = data.OpenPrice;
        }

        void ToggleTicker(object parameter)
        {
            var item = parameter as TickerWidgetCommandItem;
            if (item == null) return;

            if (item.Checked) _feed.Subscribe(item.Coin.Kind);
            else _feed.Unsubscribe(item.Coin.Kind);
        }

        public void Dispose()
        {
            _feed.Dispose();
        }
    }
}