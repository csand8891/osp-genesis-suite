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
using RuleArchitect.DesktopClient.ViewModels;

namespace RuleArchitect.DesktopClient.Views
{
    /// <summary>
    /// Interaction logic for SoftwareOptionsView.xaml
    /// </summary>
    public partial class SoftwareOptionsView : UserControl
    {
        public SoftwareOptionsView()
        {
            InitializeComponent();
            //Loaded += SoftwareOptionsView_Loaded_ForDebug; // Add this line
        }

        //private void SoftwareOptionsView_Loaded_ForDebug(object sender, RoutedEventArgs e)
        //{
        //    if (this.DataContext is SoftwareOptionsViewModel vm)
        //    {
        //        vm.PropertyChanged += (s, args) =>
        //        {
        //            if (args.PropertyName == nameof(vm.DetailPaneWidth))
        //            {
        //                // This event means the ViewModel *thinks* DetailPaneWidth changed.
        //                // Now, let's see if we can force the target (ColumnDefinition.Width) to update.
        //                System.Diagnostics.Debug.WriteLine($"DEBUG: ViewModel raised PropertyChanged for DetailPaneWidth. New GridLength from VM: {vm.DetailPaneWidth}");

        //                BindingExpression be = DetailPaneColumn.GetBindingExpression(ColumnDefinition.WidthProperty);
        //                if (be != null)
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"DEBUG: BindingExpression found for DetailPaneColumn.Width. Updating target...");
        //                    be.UpdateTarget(); // Force the binding to re-read from the source
        //                    System.Diagnostics.Debug.WriteLine($"DEBUG: After UpdateTarget(), DetailPaneColumn.Width is: {DetailPaneColumn.Width}, ActualWidth: {DetailPaneColumn.ActualWidth}");
        //                }
        //                else
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"DEBUG: NO BindingExpression found for DetailPaneColumn.Width.");
        //                }
        //            }
        //        };
        //    }
        //}
    }
}
