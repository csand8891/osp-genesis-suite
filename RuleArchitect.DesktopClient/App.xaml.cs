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
using System.Windows.Controls;

namespace RuleArchitect.DesktopClient
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        public App()
        {
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;
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
            // RE-ADDED: The SnackbarMessageQueue is required for Material Design notifications.
            services.AddSingleton(new SnackbarMessageQueue(TimeSpan.FromSeconds(3)));

            // --- ViewModels ---
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<AdminDashboardViewModel>();
            services.AddTransient<SoftwareOptionsViewModel>();
            services.AddTransient<EditSoftwareOptionViewModel>();
            services.AddTransient<AddSoftwareOptionWizardViewModel>();
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<EditSpecCodeDialogViewModel>();
            services.AddTransient<UserActivityLogViewModel>();
            services.AddTransient<OrderManagementViewModel>();
            services.AddTransient<EditOrderViewModel>();

            // --- Windows and Views ---
            services.AddTransient<LoginWindow>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<EditSpecCodeDialog>();
            services.AddTransient<AddSoftwareOptionWizardView>();
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
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Database operation failed: {ex.Message}", "Operation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown(-1);
                    return;
                }
            }

            var loginWindow = _serviceProvider.GetService<LoginWindow>();
            bool? loginResult = false;

            if (loginWindow != null)
            {
                loginResult = loginWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Critical error: Login window could not be initialized.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(-1);
                return;
            }

            if (loginResult == true)
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

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                mainWindow.DataContext = mainViewModel;
                mainWindow.Title = $"OSP Genesis Suite - ({currentUser.Role})";

                Application.Current.MainWindow = mainWindow;
                mainWindow.Show();
            }
            else
            {
                Current.Shutdown();
            }
        }
    }
}
