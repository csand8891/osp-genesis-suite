using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RuleArchitect.Abstractions.DTOs.Auth;
using RuleArchitect.Abstractions.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GenesisSentry.Services
{
    public class AuthenticationStateProvider : IAuthenticationStateProvider, INotifyPropertyChanged
    {
        private UserDto _currentUser;
        public UserDto CurrentUser
        {
            get => _currentUser;
            private set
            {
                if (_currentUser != value)
                {
                    _currentUser = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsAuthenticated));
                }
            }
        }

        public bool IsAuthenticated => CurrentUser != null;
        public void SetCurrentUser(UserDto user) => CurrentUser = user;
        public void ClearCurrentUser() => CurrentUser = null;
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
