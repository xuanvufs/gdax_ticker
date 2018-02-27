using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GDax
{
    /// <summary>
    /// Interaction logic for TickerWidget.xaml
    /// </summary>
    public partial class TickerWidget : Window
    {
        private CryptoCoin _model;

        public TickerWidget(CryptoCoin model)
        {
            InitializeComponent();
            DataContext = _model = model;
            tickerCard.Background = _model.NonActiveBackground;
        }

        private void MoveTicker(object sender, MouseButtonEventArgs e)
        {
            Native.ReleaseCapture();
            Native.SendMessage(new WindowInteropHelper(this).Handle, Native.WM_NCLBUTTONDOWN, Native.HT_CAPTION, 0);
        }

        private void MouseOver(object sender, MouseEventArgs e)
        {
            tickerCard.Background = _model.Background;
        }

        private void MouseOut(object sender, MouseEventArgs e)
        {
            tickerCard.Background = _model.NonActiveBackground;
        }
    }
}
