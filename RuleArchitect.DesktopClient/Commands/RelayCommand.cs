// In RuleArchitect.DesktopClient/Commands/RelayCommand.cs
namespace RuleArchitect.DesktopClient.Commands
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Input;

    public class RelayCommand : ICommand
    {
        private readonly Action<object?>? _execute;
        private readonly Func<object?, bool>? _canExecute;
        private readonly Func<object?, Task>? _executeAsync;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Constructor for parameterless actions
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(param => execute(),
                    canExecute == null ? (Func<object?, bool>?)null : (param => canExecute()))
        {
        }

        // Constructor for parameterless async actions
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(async param => await executeAsync(),
                    canExecute == null ? (Func<object?, bool>?)null : (param => canExecute()))
        {
        }

        // Main constructor for actions with parameters
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Main constructor for async actions with parameters
        public RelayCommand(Func<object?, Task> executeAsync, Func<object?, bool>? canExecute = null)
        {
            _executeAsync = executeAsync ?? throw new ArgumentNullException(nameof(executeAsync));
            _canExecute = canExecute;
        }


        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public async void Execute(object? parameter)
        {
            if (_execute != null)
            {
                _execute(parameter);
            }
            else if (_executeAsync != null)
            {
                await _executeAsync(parameter);
            }
        }

        /// <summary>
        /// Method to manually raise the CanExecuteChanged event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            // CommandManager.InvalidateRequerySuggested(); // This line forces the CommandManager to requery
            // While InvalidateRequerySuggested() works, a more direct way if you are not using the event directly:
            // The CanExecuteChanged event in this RelayCommand uses CommandManager.RequerySuggested.
            // To ensure an immediate update, you can call CommandManager.InvalidateRequerySuggested().
            // Most MVVM frameworks that provide RelayCommand/DelegateCommand implement this
            // by directly invoking the CanExecuteChanged event handler.
            // However, since your implementation hooks into CommandManager.RequerySuggested,
            // calling CommandManager.InvalidateRequerySuggested() is the most consistent way
            // to work with your current RelayCommand setup.
            CommandManager.InvalidateRequerySuggested();
        }
    }
}