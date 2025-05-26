// File: RuleArchitect.DesktopClient/ViewModels/BaseViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public Action? ItemChangedCallback { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            ItemChangedCallback?.Invoke();
        }

        // CORRECTED SetProperty method signature and implementation
        protected bool SetProperty<T>(
            ref T storage,
            T value,
            Action? onChanged = null, // Action to call when property actually changes
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName); // This will notify UI and call ItemChangedCallback
            onChanged?.Invoke();             // This explicitly calls the specific action passed for this property
            return true;
        }
    }
}