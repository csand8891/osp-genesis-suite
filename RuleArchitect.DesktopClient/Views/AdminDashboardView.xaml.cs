using RuleArchitect.DesktopClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RuleArchitect.DesktopClient.Views
{
    /// <summary>
    /// Interaction logic for AdminDashboardView.xaml
    /// </summary>
    public partial class AdminDashboardView : UserControl
    {
        public AdminDashboardView()
        {
            Debug.WriteLine("AdminDashboardView: Constructor START.");
            InitializeComponent();
            Debug.WriteLine("AdminDashboardView: Constructor END.");
            this.Loaded += AdminDashboardView_Loaded;
        }

        private void AdminDashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("AdminDashboardView: LOADED event fired!");
            if (this.DataContext is AdminDashboardViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"AdminDashboardView: DataContext IS AdminDashboardViewModel. IsLoading: {vm.IsLoading}");
                System.Diagnostics.Debug.WriteLine($"AdminDashboardView: LoadDashboardDataCommand is null: {vm.LoadDashboardDataCommand == null}");
                if (vm.LoadDashboardDataCommand != null)
                {
                    bool canExecute = vm.LoadDashboardDataCommand.CanExecute(null);
                    System.Diagnostics.Debug.WriteLine($"AdminDashboardView: CanExecute LoadDashboardDataCommand: {canExecute}");
                    if (!canExecute)
                    {
                        System.Diagnostics.Debug.WriteLine("AdminDashboardView: LoadDashboardDataCommand.CanExecute is false. Check IsLoading or command initialization.");
                    }
                    // The XAML Behavior should handle the command execution now.
                    // If it doesn't, then the XAML Behavior (<i:InvokeCommandAction.../>) itself has an issue.
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"AdminDashboardView: DataContext is NOT AdminDashboardViewModel. Actual DataContext: {this.DataContext?.GetType().FullName ?? "null"}");
            }
        }
    }
}
