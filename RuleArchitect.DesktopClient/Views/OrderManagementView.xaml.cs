// RuleArchitect.DesktopClient/Views/OrderManagementView.xaml.cs
using RuleArchitect.DesktopClient.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RuleArchitect.DesktopClient.Views
{
    /// <summary>
    /// Interaction logic for OrderManagementView.xaml
    /// </summary>
    public partial class OrderManagementView : UserControl
    {
        // THIS IS THE ONLY CONSTRUCTOR THAT SHOULD BE IN THIS FILE
        public OrderManagementView()
        {
            InitializeComponent();
            this.Loaded += OrderManagementView_Loaded;
        }

        private void OrderManagementView_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is OrderManagementViewModel viewModel && viewModel.LoadOrdersCommand.CanExecute(null))
            {
                viewModel.LoadOrdersCommand.Execute(null);
            }
        }
    }
}