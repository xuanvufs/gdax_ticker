using GDax.IoC;
using GDax.Models;
using GDax.Settings;
using System.Windows.Input;

namespace GDax.Commands
{
    public class TickerItem : IMenuItem
    {
        private IProduct _product;
        private ITickerSetting _settings;

        public TickerItem(ISettingsFactory factory, IFeed feed, IProduct product)
        {
            _product = product;
            _settings = factory.GetOrCreateSetting<ITickerSetting>(_product.ProductId);
            Command = new DelegateCommand(o =>
            {
                if (Checked) feed.Subscribe(_product);
                else feed.Unsubscribe(_product);
            });
        }

        public string Text => _product.ProductId;

        public string ToolTip => _product.ProductId;

        public ICommand Command { get; }

        public object Parameter => this;

        public bool Checked
        {
            get
            {
                return _settings.Subscribed;
            }
            set
            {
                _settings.Subscribed = value;
            }
        }

        public bool Checkable => true;
    }
}