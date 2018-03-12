namespace GDax.Settings
{
    public interface ITickerSetting
    {
        double Top { get; set; }
        double Left { get; set; }
        bool Subscribed { get; set; }
    }
}
