// File: RuleArchitect.Abstractions/DTOs/SoftwareOption/CreateSoftwareOptionCommandDto.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    public class CreateSoftwareOptionCommandDto : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private string _primaryName;
        private string? _alternativeNames;
        private string? _sourceFileName;
        private string? _primaryOptionNumberDisplay;
        private string? _notes;
        private int? _controlSystemId;

        public string PrimaryName
        {
            get => _primaryName;
            set { if (_primaryName != value) { _primaryName = value; OnPropertyChanged(); } }
        }

        public string? AlternativeNames
        {
            get => _alternativeNames;
            set { if (_alternativeNames != value) { _alternativeNames = value; OnPropertyChanged(); } }
        }

        public string? SourceFileName
        {
            get => _sourceFileName;
            set { if (_sourceFileName != value) { _sourceFileName = value; OnPropertyChanged(); } }
        }

        public string? PrimaryOptionNumberDisplay
        {
            get => _primaryOptionNumberDisplay;
            set { if (_primaryOptionNumberDisplay != value) { _primaryOptionNumberDisplay = value; OnPropertyChanged(); } }
        }

        public string? Notes
        {
            get => _notes;
            set { if (_notes != value) { _notes = value; OnPropertyChanged(); } }
        }

        public int? ControlSystemId
        {
            get => _controlSystemId;
            set { if (_controlSystemId != value) { _controlSystemId = value; OnPropertyChanged(); } }
        }

        // Collections for dependent entities
        public List<OptionNumberRegistryCreateDto> OptionNumbers { get; set; }
        public List<RequirementCreateDto> Requirements { get; set; }
        public List<SoftwareOptionSpecificationCodeCreateDto> SpecificationCodes { get; set; }
        public List<SoftwareOptionActivationRuleCreateDto> ActivationRules { get; set; }

        public CreateSoftwareOptionCommandDto()
        {
            OptionNumbers = new List<OptionNumberRegistryCreateDto>();
            Requirements = new List<RequirementCreateDto>();
            SpecificationCodes = new List<SoftwareOptionSpecificationCodeCreateDto>();
            ActivationRules = new List<SoftwareOptionActivationRuleCreateDto>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region INotifyDataErrorInfo Implementation (NEW)

        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public bool HasErrors => _errors.Any();

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            {
                return Enumerable.Empty<string>();
            }
            return _errors[propertyName];
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }

        // Public method to trigger validation
        public void Validate()
        {
            // Primary Name Validation
            ClearErrors(nameof(PrimaryName));
            if (string.IsNullOrWhiteSpace(PrimaryName))
            {
                AddError(nameof(PrimaryName), "Primary Name is required.");
            }

            // Control System Validation
            ClearErrors(nameof(ControlSystemId));
            if (!ControlSystemId.HasValue || ControlSystemId <= 0)
            {
                AddError(nameof(ControlSystemId), "A Control System must be selected.");
            }
        }

        private void AddError(string propertyName, string error)
        {
            if (!_errors.ContainsKey(propertyName))
                _errors[propertyName] = new List<string>();

            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }
        #endregion
    }
}
