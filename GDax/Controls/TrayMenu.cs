using GDax.Views.Models;
using System.Windows.Controls;

namespace GDax.Controls
{
    public class TrayMenu : ContextMenu
    {
        private readonly ITrayMenuViewModel _model;

        public TrayMenu(ITrayMenuViewModel model)
        {
            _model = model;

            DataContext = model;
        }
    }
}