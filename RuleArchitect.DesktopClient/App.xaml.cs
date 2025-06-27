using GenesisOrderGateway.Interfaces;
using GenesisOrderGateway.Services;
using GenesisSentry.Interfaces;
using GenesisSentry.Services;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.DTOs.Auth;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Data;
using RuleArchitect.DesktopClient.Services;
using RuleArchitect.DesktopClient.ViewModels;
using RuleArchitect.DesktopClient.Views;
using System;
using System.Windows;

namespace RuleArchitect.DesktopClient
{
    public partial class App : Application
    {
        private readonly ServiceProvider _serviceProvider;

        public App()
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown; // This is correct
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // DbContext
            services.AddDbContext<RuleArchitectContext>(options => {
            }, ServiceLifetime.Scoped);

            services.AddScoped<IAuthenticationDbContext>(provider =>
                provider.GetRequiredService<RuleArchitectContext>());

            // RuleArchitect ApplicationLogic Services
            services.AddScoped<ISoftwareOptionService, SoftwareOptionService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IUserActivityLogService, UserActivityLogService>();

            // GenesisSentry Services
            services.AddTransient<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IAuthenticationStateProvider, GenesisSentry.Services.AuthenticationStateProvider>();
            services.AddScoped<IUserService, UserService>();

            // GenesisOrderGateway Service
            services.AddTransient<IGenesisOrderGateway, PdfOrderGatewayService>();

            // Notification Service
            services.AddSingleton<HeraldKit.Interfaces.INotificationService, WpfNotificationService>();
            services.AddSingleton(new SnackbarMessageQueue(TimeSpan.FromSeconds(3)));

            // --- ViewModels ---
            services.AddTransient<LoginViewModel>();
            // MainViewModel is now transient so we can create a fresh one after re-login.
            services.AddTransient<MainViewModel>();
            services.AddTransient<AdminDashboardViewModel>();
            services.AddTransient<SoftwareOptionsViewModel>();
            services.AddTransient<EditSoftwareOptionViewModel>();
            services.AddTransient<AddSoftwareOptionWizardViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<EditSpecCodeDialogViewModel>();
            services.AddTransient<UserActivityLogViewModel>();
            services.AddTransient<OrderManagementViewModel>();


            // --- Windows and Views ---
            services.AddTransient<LoginWindow>();
            // MainWindow is now transient.
            services.AddTransient<MainWindow>();
            services.AddTransient<EditSpecCodeDialog>();
            services.AddTransient<AddSoftwareOptionWizardView>();

            services.AddTransient<CreateOrderFromPdfViewModel>();
            services.AddTransient<Views.CreateOrderFromPdfView>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RuleArchitectContext>();
                try
                {
                    context.Database.Migrate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database operation failed: {ex.Message}", "Operation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown(-1);
                    return;
                }
            }

            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginWindow = _serviceProvider.GetService<LoginWindow>();
            if (loginWindow.ShowDialog() == true)
            {
                ShowMainWindow();
            }
            else
            {
                Current.Shutdown();
            }
        }

        private void ShowMainWindow()
        {
            var authStateProvider = _serviceProvider.GetRequiredService<IAuthenticationStateProvider>();
            UserDto currentUser = authStateProvider.CurrentUser;

            if (currentUser == null)
            {
                MessageBox.Show("Login succeeded but no user information is available. Shutting down.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(-1);
                return;
            }

            var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();

            // Subscribe to the logout event
            mainViewModel.LogoutRequested += OnLogoutRequested;

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

            // Unsubscribe from the old window's Closed event if it exists, to prevent memory leaks
            mainWindow.Closed -= MainWindow_Closed;
            // Subscribe to the new window's Closed event to handle app shutdown
            mainWindow.Closed += MainWindow_Closed;

            mainWindow.DataContext = mainViewModel;
            mainWindow.Title = $"OSP Genesis Suite - ({currentUser.Role})";

            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void OnLogoutRequested()
        {
            // Close the current main window.
            // This will trigger its 'Closed' event, but we need to remove our generic shutdown handler first.
            if (Application.Current.MainWindow is MainWindow currentMain)
            {
                currentMain.Closed -= MainWindow_Closed; // Prevent shutdown
                currentMain.Close();
            }

            // Show the login window again.
            ShowLogin();
        }

        // This handler now ONLY handles the case where the user closes the main window with the 'X' button.
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
