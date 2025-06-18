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
            this.DataContextChanged += SoftwareOptionsView_DataContextChanged;
            //Loaded += SoftwareOptionsView_Loaded_ForDebug; // Add this line
        }

        private void SoftwareOptionsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SoftwareOptionsViewModel oldVm)
            {
                oldVm.DetailPaneVisibilityChanged -= ViewModel_DetailPaneVisibilityChanged;
            }
            if (e.NewValue is SoftwareOptionsViewModel newVm)
            {
                newVm.DetailPaneVisibilityChanged += ViewModel_DetailPaneVisibilityChanged;
                // Call once to set initial state based on ViewModel
                UpdateColumnWidths(newVm);
            }
        }

        private void ViewModel_DetailPaneVisibilityChanged(object sender, EventArgs e)
        {
            if (sender is SoftwareOptionsViewModel vm)
            {
                UpdateColumnWidths(vm);
            }
        }

        private void UpdateColumnWidths(SoftwareOptionsViewModel viewModel)
        {
            if (viewModel.IsDetailPaneVisible)
            {
                MasterColumn.Width = viewModel.MasterPaneColumnWidth;
                SplitterColumn.Width = viewModel.SplitterActualColumnWidth;
                DetailColumn.Width = viewModel.DetailPaneColumnWidth;
            }
            else
            {
                MasterColumn.Width = new GridLength(1, GridUnitType.Star);
                SplitterColumn.Width = new GridLength(0);
                DetailColumn.Width = new GridLength(0);
            }
            // Try to force the parent Grid to re-measure.
            // Get the parent grid of these columns (the Grid at Grid.Row="2" in your XAML)
            if (MasterColumn.Parent is Grid parentGrid)
            {
                parentGrid.UpdateLayout();
                // Or InvalidateMeasure() then UpdateLayout() if UpdateLayout alone isn't enough
                // parentGrid.InvalidateMeasure();
                // parentGrid.UpdateLayout();
            }
        }

        private void SoftwareOptionsView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is SoftwareOptionsViewModel vm)
            {
                vm.DetailPaneVisibilityChanged -= ViewModel_DetailPaneVisibilityChanged;
            }
            this.DataContextChanged -= SoftwareOptionsView_DataContextChanged; // Also unsubscribe from DataContextChanged
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
