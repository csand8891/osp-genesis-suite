// File: RuleArchitect.DesktopClient/ViewModels/BaseViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Overload to handle actions on property changed, which is used in your original code.
        protected bool SetProperty<T>(ref T storage, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
        {
            // Add this line for debugging
            System.Diagnostics.Debug.WriteLine($"SetProperty called for {propertyName}. Current value: {storage}, New value: {value}");

            if (EqualityComparer<T>.Default.Equals(storage, value))
            {
                // Add this line if it's skipping due to equality
                System.Diagnostics.Debug.WriteLine($"SetProperty for {propertyName} skipped due to equality: {storage} == {value}");
                return false;
            }

            storage = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
