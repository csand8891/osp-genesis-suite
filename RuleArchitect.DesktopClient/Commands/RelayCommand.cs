// Suggested location: RuleArchitect.DesktopClient/Commands/RelayCommand.cs
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

        // Constructor for parameterless actions
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(param => execute(),
                    canExecute == null ? (Func<object?, bool>?)null : (param => canExecute()) // Line 34 fix
                  )
        {
        }

        // Constructor for parameterless async actions
        public RelayCommand(Func<Task> executeAsync, Func<bool>? canExecute = null)
            : this(async param => await executeAsync(),
                    canExecute == null ? (Func<object?, bool>?)null : (param => canExecute()) // Line 40 fix
                  )
        {
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
    }
}