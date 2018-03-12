using GDax.Enums;
using GDax.IoC;
using GDax.Settings;
using System.Windows.Input;

namespace GDax.Commands
{
    public class TickerItem : IMenuItem
    {
        private CoinKind _kind;
        private ITickerSetting _settings;

        public TickerItem(ISettingsFactory factory, IFeed feed, CoinKind kind)
        {
            _kind = kind;
            _settings = factory.GetOrCreateSetting<ITickerSetting>(kind.ToString());
            Command = new DelegateCommand(o =>
            {
                if (Checked) feed.Subscribe(_kind);
                else feed.Unsubscribe(_kind);
            });
        }

        public string Text => _kind.ToString();

        public string ToolTip => _kind.ToString();

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