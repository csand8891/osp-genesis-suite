using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.ApplicationLogic.Interfaces;
using RuleArchitect.ApplicationLogic.Services;
using RuleArchitect.Data;
using RuleArchitect.DesktopClient.ViewModels;
using System;
using System.Windows;

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

            // Service can be Scoped as it will be resolved per operation scope
            services.AddScoped<ISoftwareOptionService, SoftwareOptionService>();

            // ViewModel remains Singleton, but it will create scopes for operations
            services.AddSingleton<SoftwareOptionsViewModel>();
            services.AddSingleton<MainWindow>();
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

            var mainWindow = _serviceProvider.GetService<MainWindow>();

            // Set the DataContext of the MainWindow to the SoftwareOptionsViewModel.
            // This enables data binding between the View (MainWindow) and the ViewModel.
            // This assumes MainWindow does not set its DataContext elsewhere or expects it via constructor.
            mainWindow.DataContext = _serviceProvider.GetService<SoftwareOptionsViewModel>();
            mainWindow.Show();
        }
    }
}