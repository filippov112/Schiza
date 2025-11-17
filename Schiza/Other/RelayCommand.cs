using System.Windows.Input;

namespace Schiza.Other
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _action;
        private readonly Func<bool>? _check;

        public RelayCommand(Action<object?> action, Func<bool>? check = null)
        {
            _action = action;
            _check = check;
        }

        public bool CanExecute(object? parameter) => _check?.Invoke() ?? true;

        public void Execute(object? parameter) => _action.Invoke(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) =>
            _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) =>
            _execute((T)parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
