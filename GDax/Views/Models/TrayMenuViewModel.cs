using GDax.Commands;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace GDax.Views.Models
{
    public interface ITrayMenuViewModel
    {
        ObservableCollection<IMenuItem> MenuItems { get; }
    }

    public class TrayMenuViewModel : ITrayMenuViewModel
    {
        public TrayMenuViewModel(List<IMenuItem> items)
        {
            MenuItems = new ObservableCollection<IMenuItem>(items);

            MenuItems.Add(null);
            MenuItems.Add(new SimpleItem
            {
                Text = "Exit",
                Command = new DelegateCommand(o => Application.Current.Shutdown())
            });
        }

        public ObservableCollection<IMenuItem> MenuItems { get; }
    }
}