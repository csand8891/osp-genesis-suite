// This can be in its own file (e.g., NavigationItemViewModel.cs)
// or defined within MainViewModel.cs if you prefer (though separate is cleaner).
// Make sure it's in the correct namespace: RuleArchitect.DesktopClient.ViewModels

using System.Windows.Input; // For ICommand
using System; // For Type
using RuleArchitect.DesktopClient.Commands; // If RelayCommand is used here, though MainViewModel sets it
using MaterialDesignThemes.Wpf; // UPDATED: Required for PackIconKind


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

        // UPDATED: Implemented the IconKind property
        private PackIconKind _iconKind;
        public PackIconKind IconKind
        {
            get => _iconKind;
            set => SetProperty(ref _iconKind, value);
        }
    }
}
