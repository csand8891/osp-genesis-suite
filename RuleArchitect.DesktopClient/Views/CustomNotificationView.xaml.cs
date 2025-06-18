using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace RuleArchitect.DesktopClient.Views
{
    public partial class CustomNotificationView : UserControl
    {
        public CustomNotificationView(string message, PackIconKind iconKind, Brush background, Brush foreground)
        {
            InitializeComponent();
            MessageTextBlock.Text = message;
            Icon.Kind = iconKind;
            Icon.Foreground = foreground;
            Card.Background = background;
            MessageTextBlock.Foreground = foreground;
        }
    }
}
