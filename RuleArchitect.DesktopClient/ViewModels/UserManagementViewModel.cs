using GenesisSentry.DTOs;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private bool _isDetailPaneVisible;
        public bool IsDetailPaneVisible
        {
            get => _isDetailPaneVisible;
            set
            {
                if (SetProperty(ref _isDetailPaneVisible, value))
                {
                    OnPropertyChanged(nameof(DetailPaneWidth));
                }
            }
        }
        public GridLength DetailPaneWidth => IsDetailPaneVisible ? new GridLength(0.8, GridUnitType.Star) : new GridLength(0);

        public ICommand ShowDetailPaneCommand { get; }
        public ICommand HideDetailPaneCommand { get; }
        public ICommand ToggleDetailPaneCommand { get; }

        private UserDto _selectedUser;
        public UserDto SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(_selectedUser, value))
                {
                    CurrentEditUser = new UserEditViewModel(_selectedUser);
                    IsDetailPaneVisible = true;
                    IsEditing = true;
                    IsAdding = false;
                }
                else
                {
                    CurrentEditUser = null;
                    IsDetailPaneVisible = false;
                    IsEditing = false;
                    IsAdding = false;    
                }
            }
        }

        UserEditViewModel CurrentEditUser;
        bool IsEditing;
        bool IsAdding;

        public UserManagementViewModel()
        {
            IsDetailPaneVisible = false;

            ShowDetailPaneCommand = new RelayCommand() => IsDetailPaneVisible = true, () => !IsDetailPaneVisible;
            HideDetailPaneCommand = new RelayCommand() =>
            {
                IsDetailPaneVisible = false;
                SelectedUser = null;
            },
            () => IsDetailPaneVisible);
            ToggleDetailPaneCommand = new RelayCommand(() => IsDetailPaneVisible = !IsDetailPaneVisible);
        }
    }
}
