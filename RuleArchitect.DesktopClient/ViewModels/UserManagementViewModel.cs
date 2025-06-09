// File: RuleArchitect.DesktopClient/ViewModels/UserManagementViewModel.cs
using RuleArchitect.Abstractions.DTOs;
using RuleArchitect.Abstractions.Interfaces;
using HeraldKit.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        private ObservableCollection<UserDto> _users;
        public ObservableCollection<UserDto> Users
        {
            get => _users;
            private set => SetProperty(ref _users, value);
        }

        private UserDto? _selectedUser;
        public UserDto? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (SetProperty(ref _selectedUser, value))
                {
                    if (_selectedUser != null)
                    {
                        CurrentEditUser = new UserEditViewModel(_selectedUser);
                        IsAdding = false;
                        IsEditing = true;
                        IsDetailPaneVisible = true;
                    }
                    else
                    {
                        // No selection, or selection cleared
                        CurrentEditUser = null;
                        IsEditing = false;
                        // Optionally hide detail pane if nothing is selected
                        // IsDetailPaneVisible = false; 
                    }
                    ((RelayCommand)EditUserCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DeleteUserCommand).RaiseCanExecuteChanged();
                }
            }
        }

        private UserEditViewModel? _currentEditUser;
        public UserEditViewModel? CurrentEditUser
        {
            get => _currentEditUser;
            set => SetProperty(ref _currentEditUser, value);
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value, UpdateCommandStates);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value, UpdateCommandStates);
        }

        private bool _isAdding;
        public bool IsAdding
        {
            get => _isAdding;
            set => SetProperty(ref _isAdding, value, UpdateCommandStates);
        }

        private bool _isDetailPaneVisible;
        public bool IsDetailPaneVisible
        {
            get => _isDetailPaneVisible;
            set
            {
                if (SetProperty(ref _isDetailPaneVisible, value))
                {
                    OnPropertyChanged(nameof(DetailPaneWidth));
                    if (!_isDetailPaneVisible) // If pane is hidden, clear editing state
                    {
                        SelectedUser = null;
                        CurrentEditUser = null;
                        IsAdding = false;
                        IsEditing = false;
                    }
                }
            }
        }
        public GridLength DetailPaneWidth => IsDetailPaneVisible ? new GridLength(0.8, GridUnitType.Star) : new GridLength(0);


        public ICommand LoadUsersCommand { get; }
        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand CancelEditCommand { get; }


        public UserManagementViewModel(IUserService userService, INotificationService notificationService)
        {
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));

            _users = new ObservableCollection<UserDto>();

            LoadUsersCommand = new RelayCommand(async () => await LoadUsersAsync(), () => !IsLoading);
            AddUserCommand = new RelayCommand(PrepareAddUser, () => !IsLoading && !IsAdding && !IsEditing);
            EditUserCommand = new RelayCommand(PrepareEditUser, () => SelectedUser != null && !IsLoading && !IsAdding && !IsEditing);
            SaveUserCommand = new RelayCommand(async () => await SaveUserAsync(), () => (IsAdding || IsEditing) && !IsLoading && CurrentEditUser != null);
            DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => SelectedUser != null && !IsLoading && !IsAdding && !IsEditing);
            CancelEditCommand = new RelayCommand(CancelEditOrAdd, () => IsAdding || IsEditing);

            // Initial load can be triggered here or by the view
            // Task.Run(async () => await LoadUsersAsync());
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)LoadUsersCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AddUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)EditUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SaveUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DeleteUserCommand).RaiseCanExecuteChanged();
            ((RelayCommand)CancelEditCommand).RaiseCanExecuteChanged();
        }

        private async Task LoadUsersAsync()
        {
            IsLoading = true;
            try
            {
                var usersList = await _userService.GetAllUsersAsync();
                Users.Clear();
                if (usersList != null)
                {
                    foreach (var user in usersList)
                    {
                        Users.Add(user);
                    }
                }
                _notificationService.ShowInformation($"{Users.Count} users loaded.", "Users Loaded");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error loading users: {ex.Message}", "Load Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrepareAddUser()
        {
            SelectedUser = null; // Clear any selection
            CurrentEditUser = new UserEditViewModel();
            IsAdding = true;
            IsEditing = false;
            IsDetailPaneVisible = true;
        }

        private void PrepareEditUser()
        {
            if (SelectedUser == null) return;
            CurrentEditUser = new UserEditViewModel(SelectedUser);
            IsAdding = false;
            IsEditing = true;
            IsDetailPaneVisible = true;
        }

        private async Task SaveUserAsync()
        {
            if (CurrentEditUser == null) return;

            if (!CurrentEditUser.ValidateInput(out List<string> validationErrors))
            {
                _notificationService.ShowWarning(string.Join(Environment.NewLine, validationErrors), "Validation Failed");
                return;
            }

            IsLoading = true;
            try
            {
                if (CurrentEditUser.IsNewUser)
                {
                    var newUserDto = await _userService.CreateUserAsync(
                        CurrentEditUser.UserName,
                        CurrentEditUser.Password!, // Validation ensures Password is not null for new user
                        CurrentEditUser.Role,
                        CurrentEditUser.IsActive);

                    if (newUserDto != null)
                    {
                        Users.Add(newUserDto);
                        SelectedUser = newUserDto; // Select the newly added user
                        _notificationService.ShowSuccess($"User '{newUserDto.UserName}' created successfully.", "User Created");
                        IsAdding = false;
                        IsDetailPaneVisible = false; // Or keep open to show the new user
                    }
                }
                else // Editing existing user
                {
                    var updateUserDto = new UpdateUserDto
                    {
                        UserId = CurrentEditUser.UserId,
                        UserName = CurrentEditUser.UserName,
                        Role = CurrentEditUser.Role,
                        IsActive = CurrentEditUser.IsActive,
                        // Only include password if it's being changed
                        Password = string.IsNullOrWhiteSpace(CurrentEditUser.Password) ? null : CurrentEditUser.Password
                    };

                    var updatedUser = await _userService.UpdateUserAsync(updateUserDto);
                    if (updatedUser != null)
                    {
                        // Refresh the list or update the specific item
                        await LoadUsersAsync(); // Simplest way to refresh
                        SelectedUser = Users.FirstOrDefault(u => u.UserId == updatedUser.UserId); // Re-select
                        _notificationService.ShowSuccess($"User '{updatedUser.UserName}' updated successfully.", "User Updated");
                        IsEditing = false;
                        IsDetailPaneVisible = false; // Or keep open
                    }
                }
            }
            catch (ArgumentNullException argEx) // Catch specific exceptions from UserService for better messages
            {
                _notificationService.ShowError($"Validation error: {argEx.Message}", "Save Error");
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error saving user: {ex.Message}", "Save Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteUserAsync()
        {
            if (SelectedUser == null) return;

            // Confirmation dialog
            if (MessageBox.Show($"Are you sure you want to delete user '{SelectedUser.UserName}'?",
                                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            IsLoading = true;
            try
            {
                bool success = await _userService.DeleteUserAsync(SelectedUser.UserId);
                if (success)
                {
                    Users.Remove(SelectedUser);
                    SelectedUser = null; // Clear selection
                    _notificationService.ShowSuccess("User deleted successfully.", "User Deleted");
                    IsDetailPaneVisible = false;
                }
                else
                {
                    _notificationService.ShowError("Failed to delete user. They might be referenced elsewhere or no longer exist.", "Delete Failed");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Error deleting user: {ex.Message}", "Delete Error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEditOrAdd()
        {
            CurrentEditUser = null;
            IsAdding = false;
            IsEditing = false;
            IsDetailPaneVisible = false;
            // Optionally, if a user was selected for edit, re-select them to show their original details
            // This might require storing the original SelectedUser before starting an edit.
            // For now, just clearing selection is simpler.
            // SelectedUser = null; 
        }
    }
}