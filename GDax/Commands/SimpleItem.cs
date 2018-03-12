using System.Windows.Input;

namespace GDax.Commands
{
    public class SimpleItem : IMenuItem
    {
        public string Text { get; set; }
        public string ToolTip { get; set; }
        public ICommand Command { get; set; }
        public object Parameter { get; set; }
        public bool Checkable { get; set; }
        public bool Checked { get; set; }
    }
}