// File: RuleArchitect.DesktopClient/ViewModels/UserEditViewModel.cs
using GenesisSentry.DTOs;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class UserEditViewModel : BaseViewModel
    {
        private int _userId;
        private string _userName = string.Empty;
        private string _role = string.Empty;
        private bool _isActive;
        private string? _password; // Nullable for editing existing user without changing password
        private string? _confirmPassword;

        private bool _isNewUser;

        public int UserId { get => _userId; set => SetProperty(ref _userId, value); }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public string? Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string? ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public bool IsNewUser { get => _isNewUser; private set => SetProperty(ref _isNewUser, value); }

        // Available roles for a ComboBox, for example
        public List<string> AvailableRoles { get; } = new List<string> { "Administrator", "OrderReview", "OrderProduction", "ProductionReview", "User" }; // Add other roles as needed

        /// <summary>
        /// Constructor for creating a new user.
        /// </summary>
        public UserEditViewModel()
        {
            IsNewUser = true;
            IsActive = true; // Default for new users
            Role = AvailableRoles.FirstOrDefault() ?? string.Empty; // Default role
        }

        /// <summary>
        /// Constructor for editing an existing user.
        /// </summary>
        /// <param name="userToEdit">The UserDto of the user to edit.</param>
        public UserEditViewModel(UserDto userToEdit)
        {
            if (userToEdit == null) throw new ArgumentNullException(nameof(userToEdit));

            IsNewUser = false;
            UserId = userToEdit.UserId;
            UserName = userToEdit.UserName;
            Role = userToEdit.Role;
            IsActive = userToEdit.IsActive;
            // Password and ConfirmPassword are intentionally left null/empty for existing users
            // to indicate "no change" unless explicitly set.
        }

        public bool ValidateInput(out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(UserName))
            {
                errors.Add("Username cannot be empty.");
            }
            // Regex for username complexity can be added here

            if (string.IsNullOrWhiteSpace(Role))
            {
                errors.Add("Role must be selected.");
            }

            if (IsNewUser || !string.IsNullOrWhiteSpace(Password)) // Validate password only if it's a new user or password is being changed
            {
                if (string.IsNullOrWhiteSpace(Password))
                {
                    errors.Add("Password cannot be empty for a new user or when changing it.");
                }
                else
                {
                    // Basic password policy (can be expanded)
                    if (Password.Length < 6)
                    {
                        errors.Add("Password must be at least 6 characters long.");
                    }
                    if (Password != ConfirmPassword)
                    {
                        errors.Add("Passwords do not match.");
                    }
                }
            }
            return !errors.Any();
        }
    }
}