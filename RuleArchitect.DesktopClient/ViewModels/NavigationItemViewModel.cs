// This can be in its own file (e.g., NavigationItemViewModel.cs)
// or defined within MainViewModel.cs if you prefer (though separate is cleaner).
// Make sure it's in the correct namespace: RuleArchitect.DesktopClient.ViewModels

using System.Windows.Input; // For ICommand
using System; // For Type
using RuleArchitect.DesktopClient.Commands; // If RelayCommand is used here, though MainViewModel sets it


namespace RuleArchitect.DesktopClient.ViewModels
{
    public class NavigationItemViewModel : BaseViewModel // Assuming BaseViewModel for INPC
    {
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public Type TargetViewModelType { get; set; }

        private ICommand _navigateCommand;
        public ICommand NavigateCommand
        {
            get => _navigateCommand;
            set => SetProperty(ref _navigateCommand, value);
        }

        // Optional: For icons in the navbar
        // public materialDesign:PackIconKind IconKind { get; set; }
    }
}