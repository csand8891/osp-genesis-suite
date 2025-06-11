using GenesisOrderGateway.Interfaces; // For IGenesisOrderGateway
using GenesisOrderGateway.Services;  // For PdfOrderGatewayService
using RuleArchitect.Abstractions.DTOs.Auth;
using GenesisSentry.Interfaces;
using GenesisSentry.Services;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Data;
using RuleArchitect.DesktopClient.Services;
using RuleArchitect.DesktopClient.ViewModels; // For LoginViewModel, MainViewModel, AdminDashboardViewModel, etc.
using RuleArchitect.DesktopClient.Views;
using System;
using System.Windows;

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
                // Your RuleArchitectContext.OnConfiguring should handle the connection string
                // if options are not configured here.
            }, ServiceLifetime.Scoped);

            // Register RuleArchitectContext as IAuthenticationDbContext for GenesisSentry
            services.AddScoped<IAuthenticationDbContext>(provider =>
                provider.GetRequiredService<RuleArchitectContext>());

            // RuleArchitect ApplicationLogic Services
            services.AddScoped<ISoftwareOptionService, SoftwareOptionService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IUserActivityLogService, UserActivityLogService>();
            // To be created: services.AddScoped<IUserService, UserService>();

            // GenesisSentry Services
            services.AddTransient<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<IAuthenticationStateProvider, GenesisSentry.Services.AuthenticationStateProvider>();
            services.AddScoped<IUserService, UserService>();

            // GenesisOrderGateway Service
            services.AddTransient<IGenesisOrderGateway, PdfOrderGatewayService>();
            
            // HeraldKit Notification Service (example, replace with your actual implementation if you have one)
            services.AddSingleton<HeraldKit.Interfaces.INotificationService, WpfNotificationService>();
            services.AddSingleton <SnackbarMessageQueue>(new SnackbarMessageQueue(TimeSpan.FromSeconds(3)));


            // --- ViewModels ---
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<MainViewModel>();      // MainViewModel is likely a singleton for the app's main shell
            services.AddTransient<AdminDashboardViewModel>(); // Specific dashboard/view ViewModels can be transient or scoped
            services.AddTransient<SoftwareOptionsViewModel>();
            services.AddTransient<EditSoftwareOptionViewModel>();
            // Register other ViewModels for your different views/UserControls as you create them:
            services.AddTransient<UserManagementViewModel>();
            services.AddTransient<EditSpecCodeDialogViewModel>();
            services.AddTransient<UserActivityLogViewModel>();
            // services.AddTransient<OrdersViewModel>();
            // services.AddTransient<ReportsViewModel>();
            // services.AddTransient<OrderReviewDashboardViewModel>(); 
            // etc.


            // --- Windows and Views (Views are often instantiated by DataTemplates) ---
            services.AddTransient<LoginWindow>();
            services.AddSingleton<MainWindow>(); // MainWindow is the shell, likely singleton
            services.AddTransient<EditSpecCodeDialog>();



        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Apply migrations
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<RuleArchitectContext>();
                try
                {
                    context.Database.Migrate();
                    // If you were using Method #2 for seeding (code-based after migration):
                    // var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                    // SeedAdminUserData(context, passwordHasher); // Your seeding method
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Database operation failed: {ex.Message}",
                                    "Operation Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    Current.Shutdown(-1);
                    return;
                }
            }

            // --- Login Flow ---
            var loginWindow = _serviceProvider.GetService<LoginWindow>();
            bool? loginResult = false;

            if (loginWindow != null)
            {
                loginResult = loginWindow.ShowDialog();
                System.Diagnostics.Debug.WriteLine($"App.OnStartup: After LoginWindow.ShowDialog(). Number of open windows: {Application.Current.Windows.Count}");
                foreach (Window w in Application.Current.Windows)
                {
                    System.Diagnostics.Debug.WriteLine($"App.OnStartup: Open window (post-LoginWindow): Type={w.GetType().FullName}, Title='{w.Title}', IsVisible={w.IsVisible}, IsActive={w.IsActive}");
                }
            }
            else
            {
                MessageBox.Show("Critical error: Login window could not be initialized.", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown(-1);
                return;
            }

            if (loginResult == true)
            {
                // Login was successful
                var authStateProvider = _serviceProvider.GetRequiredService<IAuthenticationStateProvider>();
                UserDto currentUser = authStateProvider.CurrentUser;

                if (currentUser == null) // Should not happen if loginResult is true and authStateProvider is working
                {
                    MessageBox.Show("Login succeeded but no user information is available. Shutting down.", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown(-1);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"App.OnStartup: User '{currentUser.UserName}' role '{currentUser.Role}'. Resolving MainViewModel.");
                var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>(); // Calls MainViewModel constructor
                System.Diagnostics.Debug.WriteLine("App.OnStartup: MainViewModel resolved. Resolving MainWindow.");

                var mainWindow = _serviceProvider.GetRequiredService<MainWindow>(); // Calls MainWindow constructor
                System.Diagnostics.Debug.WriteLine("App.OnStartup: MainWindow resolved. Setting DataContext and Title.");

                mainWindow.DataContext = mainViewModel;
                mainWindow.Title = $"OSP Genesis Suite - ({currentUser.Role})";

                Application.Current.MainWindow = mainWindow;
                System.Diagnostics.Debug.WriteLine("App.OnStartup: Application.Current.MainWindow set. Calling Show().");
                mainWindow.Show();
                System.Diagnostics.Debug.WriteLine("App.OnStartup: mainWindow.Show() CALL COMPLETED OR EXCEPTION THROWN DURING SHOW.");
            }
            else
            {
                // Login failed or was cancelled
                Current.Shutdown();
            }
        }

        // Example seed method if you were using code-based seeding (Method #2)
        // private void SeedAdminUserData(RuleArchitectContext context, IPasswordHasher passwordHasher)
        // {
        //     if (!context.Users.Any(u => u.UserName == "admin"))
        //     {
        //         string defaultPassword = "YourSecureDefaultPassword123!";
        //         var (hash, salt) = passwordHasher.HashPassword(defaultPassword);
        //         var adminUser = new UserEntity { /* ... details ... */ };
        //         context.Users.Add(adminUser);
        //         context.SaveChanges();
        //     }
        // }
    }
}