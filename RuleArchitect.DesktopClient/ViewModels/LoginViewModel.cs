// In RuleArchitect.DesktopClient/ViewModels/LoginViewModel.cs
using GenesisSentry.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly IAuthenticationService _authService;
        private readonly IAuthenticationStateProvider _authStateProvider;
        private string _username = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoggingIn;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); ((RelayCommand)LoginCommand).RaiseCanExecuteChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set { _isLoggingIn = value; OnPropertyChanged(); ((RelayCommand)LoginCommand).RaiseCanExecuteChanged(); }
        }

        public ICommand LoginCommand { get; }

        // We need a way to pass the password securely.
        // A common pattern is to have the View pass a SecureString or handle password retrieval.
        // For simplicity here, we'll assume a method `GetPassword` exists.
        // A better approach involves a PasswordBox helper or passing the Window to the command.
        public delegate string GetPasswordDelegate();
        public GetPasswordDelegate? GetPassword { get; set; }

        public delegate void LoginSuccessDelegate();
        public event LoginSuccessDelegate? OnLoginSuccess;

        public event EventHandler? LoginFailedErrorOccurred;


        public LoginViewModel(IAuthenticationService authService, IAuthenticationStateProvider authStateProvider)
        {
            _authService = authService;
            _authStateProvider = authStateProvider;
            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
        }

        // In LoginViewModel.cs
        private bool CanExecuteLogin()
        {
            System.Diagnostics.Debug.WriteLine($"CanExecuteLogin Check: User='{Username}', IsLoggingIn='{IsLoggingIn}', GetPasswordSet='{GetPassword != null}'");
            string? password = GetPassword?.Invoke();
            System.Diagnostics.Debug.WriteLine($"Password from GetPassword: '{password}'");

            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(password) &&
                   !IsLoggingIn &&
                   GetPassword != null;
        }

        private async Task ExecuteLoginAsync()
        {
            IsLoggingIn = true;
            ErrorMessage = string.Empty;
            string password = GetPassword?.Invoke() ?? string.Empty;

            try
            {
                var result = await _authService.AuthenticateAsync(Username, password);
                if (result.IsSuccess)
                {
                    _authStateProvider.SetCurrentUser(result.User);
                    OnLoginSuccess?.Invoke(); // Signal success to the view
                }
                else
                {
                    ErrorMessage = result.ErrorMessage ?? "Login failed.";
                    LoginFailedErrorOccurred?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                LoginFailedErrorOccurred?.Invoke(this, EventArgs.Empty);
            }
            finally
            {
                IsLoggingIn = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}