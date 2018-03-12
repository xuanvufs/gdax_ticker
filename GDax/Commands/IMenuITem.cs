using System.Windows.Input;

namespace GDax.Commands
{ 
    public interface IMenuItem
    {
        string Text { get; }
        string ToolTip { get; }
        ICommand Command { get; }
        object Parameter { get; }
        bool Checkable { get; }
        bool Checked { get; }
    }
}