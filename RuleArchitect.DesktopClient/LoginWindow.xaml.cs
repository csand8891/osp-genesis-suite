using RuleArchitect.DesktopClient.ViewModels;
using System.Windows;

namespace RuleArchitect.DesktopClient
{
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel) // Inject the ViewModel
        {
            InitializeComponent();
            DataContext = viewModel;

            // --- Password Handling & Closing Logic ---
            if (viewModel != null)
            {
                // Pass a delegate to the ViewModel to get the password
                viewModel.GetPassword = () => PasswordBox.Password; // Basic example (not SecureString)

                // Subscribe to the ViewModel's success event to close the window
                viewModel.OnLoginSuccess += () =>
                {
                    this.DialogResult = true; // Set DialogResult to indicate success
                    this.Close();
                };
            }
        }
    }
}