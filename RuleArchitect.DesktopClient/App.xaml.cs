using RuleArchitect.Data;    // Namespace of your RuleArchitectContext
using System;                // For Exception
// using System.Data.Entity; // REMOVE THIS - EF6 specific
using System.Diagnostics;      // For Debug.WriteLine
using System.IO;               // For Path and File operations
using System.Linq;             // For FirstOrDefault() - if still used by other logic in this file
using System.Windows;
// using System.Reflection; // Keep if used for other purposes, not directly for EF Core initialization here
using Microsoft.EntityFrameworkCore; // ADD THIS - For EF Core functionality

namespace RuleArchitect.DesktopClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Debug.WriteLine("Application OnStartup: Initializing database with Entity Framework Core.");

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string dbFileName = "RuleArchitect.sqlite"; // Should match connection string in App.config
            string dbFilePath = Path.Combine(baseDirectory, dbFileName);
            Debug.WriteLine($"Expected database file path: {dbFilePath}");

            if (!File.Exists(dbFilePath))
            {
                Debug.WriteLine($"Database file '{dbFilePath}' does NOT exist. EF Core Migrations should create it.");
            }
            else
            {
                Debug.WriteLine($"Database file '{dbFilePath}' exists.");
            }

            try
            {
                Debug.WriteLine("Attempting to apply EF Core migrations and initialize database context...");
                using (var context = new RuleArchitectContext()) // Assuming RuleArchitectContext is accessible
                {
                    // In EF Core, this applies pending migrations and creates the database if it doesn't exist.
                    context.Database.Migrate();
                    Debug.WriteLine("EF Core context.Database.Migrate() completed.");

                    // Optional: Re-check file existence if critical
                    if (File.Exists(dbFilePath))
                    {
                        Debug.WriteLine($"Database file '{dbFilePath}' exists after Migrate call.");
                    }
                    else if (context.Database.CanConnect()) // Check if DB is connectable (e.g. in-memory wasn't intended)
                    {
                        Debug.WriteLine($"WARNING: Database file '{dbFilePath}' does NOT exist after Migrate call, but database is connectable. Review configuration if a file was expected.");
                    }
                    else
                    {
                        Debug.WriteLine($"CRITICAL: Database file '{dbFilePath}' STILL does NOT exist after Migrate call AND cannot connect. Check permissions or EF configuration.");
                        MessageBox.Show($"CRITICAL: Database file '{dbFilePath}' was NOT created or connectable by EF Core. Check permissions or EF configuration.", "DB File Missing/Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        Current.Shutdown(-1);
                        return;
                    }

                    // Example query to confirm context is working after migration
                    Debug.WriteLine("Attempting to query MachineTypes.Count()...");
                    var count = context.MachineTypes.Count(); // Requires using System.Linq;
                    Debug.WriteLine($"MachineTypes.Count() successful. Count: {count}.");
                    MessageBox.Show($"Database initialization with EF Core migrations complete. MachineTypes count: {count}. Database should be up-to-date.", "DB Status");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EXCEPTION during EF Core database initialization/migration: {ex.ToString()}");
                string fullError = $"Error during EF Core database initialization/migration: {ex.Message}\n";
                Exception? inner = ex.InnerException; // Using nullable reference type (C# 8.0+)
                int innerCount = 0;
                while (inner != null && innerCount < 5)
                {
                    fullError += $"Inner: {inner.Message}\n";
                    Debug.WriteLine($"INNER EXCEPTION ({innerCount}): {inner.ToString()}");
                    inner = inner.InnerException;
                    innerCount++;
                }
                MessageBox.Show(fullError,
                                "Database Initialization Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                Current.Shutdown(-1); // Consider if shutdown is always appropriate
                return;
            }
            finally
            {
                Debug.WriteLine("OnStartup EF Core database initialization section finished.");
            }
        }
    }
}