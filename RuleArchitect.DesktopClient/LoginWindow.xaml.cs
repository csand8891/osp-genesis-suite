using RuleArchitect.DesktopClient.Commands;
using RuleArchitect.DesktopClient.ViewModels;
using System.Windows;
using System.Windows.Controls; // Required for TextChangedEventArgs
using System.Windows.Media.Animation;
using System;
using MaterialDesignThemes.Wpf; // Required for PackIcon

namespace RuleArchitect.DesktopClient
{
    public partial class LoginWindow : Window
    {
        private bool _isWindowLoaded = false;
        private LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            if (_viewModel != null)
            {
                _viewModel.GetPassword = () => PasswordBox.Password;
                _viewModel.OnLoginSuccess += ViewModel_OnLoginSuccess;
                _viewModel.LoginFailedErrorOccurred += ViewModel_LoginFailedErrorOccurred;

                // Subscribe to events
                PasswordBox.PasswordChanged += PasswordBox_PasswordChanged;
                PasswordTextBox.TextChanged += PasswordTextBox_TextChanged; // <-- ADDED
                PasswordVisibilityToggle.Checked += PasswordVisibility_Changed; // <-- ADDED
                PasswordVisibilityToggle.Unchecked += PasswordVisibility_Changed; // <-- ADDED
            }

            this.Loaded += LoginWindow_Loaded;
            this.Closed += LoginWindow_Closed;
        }

        private void ViewModel_OnLoginSuccess()
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ViewModel_LoginFailedErrorOccurred(object sender, EventArgs e)
        {
            if (FindResource("ShakeErrorStoryboard") is Storyboard shakeStoryboard)
            {
                shakeStoryboard.Begin(this.ErrorMessageTextBlock);
            }
        }

        // --- NEW AND UPDATED METHODS FOR PASSWORD TOGGLE ---

        private void PasswordVisibility_Changed(object sender, RoutedEventArgs e)
        {
            if (PasswordVisibilityToggle.IsChecked == true)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordTextBox.Visibility = Visibility.Visible;
                PasswordBox.Visibility = Visibility.Collapsed;
                if (PasswordVisibilityToggle.Content is PackIcon icon)
                {
                    icon.Kind = PackIconKind.EyeOff;
                }
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                if (PasswordVisibilityToggle.Content is PackIcon icon)
                {
                    icon.Kind = PackIconKind.Eye;
                }
            }
        }

        private void PasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Keep the underlying PasswordBox in sync with the visible TextBox
            if (PasswordBox.Password != PasswordTextBox.Text)
            {
                PasswordBox.Password = PasswordTextBox.Text;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Keep the visible TextBox in sync with the underlying PasswordBox
            if (PasswordTextBox.Text != PasswordBox.Password)
            {
                PasswordTextBox.Text = PasswordBox.Password;
            }

            // Original logic to update command state
            if (_isWindowLoaded && DataContext is LoginViewModel vm && vm.LoginCommand is RelayCommand loginRelayCommand)
            {
                loginRelayCommand.RaiseCanExecuteChanged();
            }
        }

        // --- EXISTING METHODS ---

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _isWindowLoaded = true;
            if (DataContext is LoginViewModel vm && vm.LoginCommand is RelayCommand rc)
            {
                rc.RaiseCanExecuteChanged();
            }
        }

        private void LoginWindow_Closed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("LoginWindow_Closed: Unsubscribing events and clearing references.");

            // Unsubscribe from events to prevent memory leaks
            PasswordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            PasswordTextBox.TextChanged -= PasswordTextBox_TextChanged; // <-- ADDED
            PasswordVisibilityToggle.Checked -= PasswordVisibility_Changed; // <-- ADDED
            PasswordVisibilityToggle.Unchecked -= PasswordVisibility_Changed; // <-- ADDED

            if (_viewModel != null)
            {
                _viewModel.OnLoginSuccess -= ViewModel_OnLoginSuccess;
                _viewModel.LoginFailedErrorOccurred -= ViewModel_LoginFailedErrorOccurred;
                _viewModel.GetPassword = null;
            }

            DataContext = null;
            _viewModel = null;
        }
    }
}