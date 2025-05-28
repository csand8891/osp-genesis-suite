using System;
using System.Collections.Generic;
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

namespace RuleArchitect.DesktopClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            System.Diagnostics.Debug.WriteLine("MainWindow Constructor - After InitializeComponent");

            // ****** ADD THIS LINE: ******
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            //this.Closing += MainWindow_Closing;
            //this.Closed += MainWindow_Closed;
            // *****************************
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow - Loaded event fired!");
            // Breakpoint here
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("MainWindow_Closed: MainWindow has been closed.");
            Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                int windowCount = Application.Current.Windows.Count;
                System.Diagnostics.Debug.WriteLine($"MainWindow_Closed (after Dispatcher.Invoke): Number of open windows: {windowCount}");
                if (windowCount > 0)
                {
                    foreach (Window w_lingering in Application.Current.Windows)
                    {
                        System.Diagnostics.Debug.WriteLine($"MainWindow_Closed (after Dispatcher.Invoke): Lingering window: Type={w_lingering.GetType().FullName}, Title='{w_lingering.Title}', IsVisible={w_lingering.IsVisible}, IsActive={w_lingering.IsActive}");
                    }
                    System.Diagnostics.Debug.WriteLine("MainWindow_Closed: THERE ARE STILL LINGERING WINDOWS. THIS IS WHY THE APP IS NOT SHUTTING DOWN with OnLastWindowClose.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MainWindow_Closed: No lingering windows. App should be shutting down with OnLastWindowClose.");
                }
            }));
        }
    }
}