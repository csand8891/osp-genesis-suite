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

        private void AdminDashboardView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Debug.WriteLine("AdminDashboardView: LOADED event fired!");
        }
    }
}
