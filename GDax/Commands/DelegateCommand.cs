using System;
using System.Windows.Input;

namespace GDax.Commands
{
    public delegate void CommandHandler(object parameter);

    public class DelegateCommand : ICommand
    {
        private readonly CommandHandler _handler;
        private readonly Predicate<object> _canExecute;
        public event EventHandler CanExecuteChanged;

        public DelegateCommand(CommandHandler handler) : this(handler, null)
        {
        }

        public DelegateCommand(CommandHandler handler, Predicate<object> canExecute)
        {
            _handler = handler;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;

            return _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _handler?.Invoke(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
