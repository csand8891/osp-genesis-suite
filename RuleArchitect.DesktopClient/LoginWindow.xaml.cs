using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.ViewModels;
using System.Windows;
using System; // Required for EventArgs

namespace RuleArchitect.DesktopClient
{
    public partial class LoginWindow : Window
    {
        private bool _isWindowLoaded = false; // Flag to track if the window has loaded
        private LoginViewModel _viewModel; // Store a reference to the ViewModel

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel; // Store the viewModel
            DataContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.GetPassword = () => PasswordBox.Password;
                _viewModel.OnLoginSuccess += ViewModel_OnLoginSuccess; // Use named handler
                PasswordBox.PasswordChanged += PasswordBox_PasswordChanged; // Use named handler
            }

            this.Loaded += LoginWindow_Loaded;
            this.Closed += LoginWindow_Closed; // Subscribe to the Closed event
        }

        private void ViewModel_OnLoginSuccess() // Named event handler
        {
            this.DialogResult = true;
            this.Close();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e) // Named event handler
        {
            // Only raise CanExecuteChanged if the window is fully loaded and DataContext is correct
            if (_isWindowLoaded && DataContext is LoginViewModel vm && vm.LoginCommand is RelayCommand loginRelayCommand)
            {
                loginRelayCommand.RaiseCanExecuteChanged();
            }
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isWindowLoaded = true;
            // After the window is loaded, explicitly update the command's CanExecute state once.
            // This ensures the button's initial state is correct based on initial (likely empty) values.
            if (DataContext is LoginViewModel vm && vm.LoginCommand is RelayCommand rc)
            {
                rc.RaiseCanExecuteChanged();
            }
        }

        private void LoginWindow_Closed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("LoginWindow_Closed: Unsubscribing events and clearing references.");

            // Unsubscribe from PasswordBox event
            PasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;

            // Unsubscribe from ViewModel event and clear delegates/references
            if (_viewModel != null)
            {
                _viewModel.OnLoginSuccess -= ViewModel_OnLoginSuccess;
                _viewModel.GetPassword = null; // Clear the delegate reference

                // If LoginViewModel implemented IDisposable or had a specific Cleanup method, call it here:
                // if (_viewModel is IDisposable disposableVm)
                // {
                //     disposableVm.Dispose();
                // }
                // _viewModel.Cleanup(); // Or a custom cleanup method
            }

            // It's also good practice to clear the DataContext, though the window is closing.
            DataContext = null;
            _viewModel = null; // Clear the stored ViewModel reference
        }
    }
}