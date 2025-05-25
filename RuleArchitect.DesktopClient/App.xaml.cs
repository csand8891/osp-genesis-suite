using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Data;
using RuleArchitect.DesktopClient.ViewModels;
using System;
using System.Windows;
using GenesisSentry.Interfaces;
using GenesisSentry.Services;

namespace RuleArchitect.DesktopClient
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// Manages the overall lifecycle and setup of the RuleArchitect desktop application,
    /// including dependency injection, database migrations, and main window initialization.
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// Sets up the dependency injection container.
        /// </summary>
        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Configures the services for the application and registers them with the dependency injection container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
        // In RuleArchitect.DesktopClient/App.xaml.cs
        private void ConfigureServices(IServiceCollection services)
        {
            // DbContext is typically Scoped
            services.AddDbContext<RuleArchitectContext>(options => {
                // Ensure your RuleArchitectContext.OnConfiguring uses the
                // connection string from App.config if options aren't configured here.
            }, ServiceLifetime.Scoped); // Explicitly Scoped

            // Register RuleArchitectContext as IAuthenticationDbContext
            services.AddScoped<IAuthenticationDbContext>(provider =>
                provider.GetRequiredService<RuleArchitectContext>());

            // Service can be Scoped as it will be resolved per operation scope
            services.AddScoped<ISoftwareOptionService, SoftwareOptionService>();

            // Register GenesisSentry Services
            services.AddTransient<IPasswordHasher, PasswordHasher>(); // <-- Add this
            services.AddScoped<IAuthenticationService, AuthenticationService>(); // <-- Add this
            services.AddSingleton<IAuthenticationStateProvider, GenesisSentry.Services.AuthenticationStateProvider>(); // <-- Add this

            // ViewModel remains Singleton, but it will create scopes for operations
            services.AddSingleton<SoftwareOptionsViewModel>();
            services.AddTransient<LoginViewModel>();
            services.AddSingleton<MainWindow>();
            services.AddTransient<LoginWindow>();
        }

        /// <summary>
        /// Raises the <see cref="Application.Startup"/> event.
        /// This method is called when the application is starting up.
        /// It handles database migrations and the display of the main application window.
        /// </summary>
        /// <param name="e">A <see cref="StartupEventArgs"/> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Apply any pending Entity Framework migrations to the database.
            using (var context = _serviceProvider.GetService<RuleArchitectContext>())
            {
                try
                {
                    context.Database.Migrate();
                    // Optional: Further database initialization or seeding logic can go here.
                }
                catch (System.Exception ex)
                {
                    // Present a user-friendly error message if database migration fails.
                    // Logging the full exception (ex) to a file or logging service is also recommended for debugging.
                    MessageBox.Show($"Database migration failed: {ex.Message}",
                                    "Migration Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                    // Depending on the severity, consider shutting down the application.
                    // Current.Shutdown(-1);
                    // return;
                }
            }
            // --- NEW: Login Flow ---
            var loginWindow = _serviceProvider.GetService<LoginWindow>(); // Assuming registered
            var loginViewModel = _serviceProvider.GetService<LoginViewModel>(); // Assuming registered

            // This part needs refinement: How to pass the password and close/show windows.
            // A common pattern:
            bool? loginResult = false;
            loginViewModel.OnLoginSuccess += () => {
                loginResult = true;
                loginWindow.Close();
            };
            // Need to handle GetPassword, e.g., pass loginWindow.PasswordBox to ViewModel or use a helper.
            // loginViewModel.GetPassword = () => loginWindow.PasswordBox.Password; // Simplified example

            loginWindow.DataContext = loginViewModel;
            loginWindow.ShowDialog(); // Show modally

            if (loginResult == true)
            {
                // Login successful, show main window
                var mainWindow = _serviceProvider.GetService<MainWindow>();
                mainWindow.DataContext = _serviceProvider.GetService<SoftwareOptionsViewModel>();
                mainWindow.Show();
            }
            else
            {
                // Login failed or cancelled, shutdown application
                Current.Shutdown();
            }
        }
    }
}