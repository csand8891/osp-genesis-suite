using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.DesktopClient.ViewModels
{
    /// <summary>
    /// ViewModel representing an Option Number in the UI.
    /// </summary>
    public class OptionNumberViewModel : BaseViewModel
    {
        private string _optionNumber = string.Empty;
        private int _originalId; // Useful if mapping back for updates/deletes, or for key in lists

        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value); }

        public string OptionNumber
        {
            get => _optionNumber;
            set => SetProperty(ref _optionNumber, value);
        }

        // Example constructor for mapping from an entity or DTO
        // public OptionNumberViewModel(int id, string number)
        // {
        //     _originalId = id;
        //     _optionNumber = number;
        // }
    }
}