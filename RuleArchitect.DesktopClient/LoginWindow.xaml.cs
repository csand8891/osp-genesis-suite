using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.ViewModels;
using System.Windows;

namespace RuleArchitect.DesktopClient
{
    public partial class LoginWindow : Window
    {
        private bool _isWindowLoaded = false; // Flag to track if the window has loaded

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            if (viewModel != null)
            {
                viewModel.GetPassword = () => PasswordBox.Password; // PasswordBox is from your XAML

                viewModel.OnLoginSuccess += () =>
                {
                    this.DialogResult = true;
                    this.Close();
                };

                // Modified PasswordChanged handler
                PasswordBox.PasswordChanged += (s, e) =>
                {
                    // Only raise CanExecuteChanged if the window is fully loaded
                    if (_isWindowLoaded && viewModel.LoginCommand is RelayCommand loginRelayCommand)
                    {
                        loginRelayCommand.RaiseCanExecuteChanged();
                    }
                };
            }

            // Subscribe to the Loaded event of the window
            this.Loaded += LoginWindow_Loaded;
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
    }
}