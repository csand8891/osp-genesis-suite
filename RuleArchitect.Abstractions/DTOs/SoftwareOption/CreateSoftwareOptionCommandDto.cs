// File: RuleArchitect.Abstractions/DTOs/SoftwareOption/CreateSoftwareOptionCommandDto.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RuleArchitect.Abstractions.DTOs.SoftwareOption
{
    public class CreateSoftwareOptionCommandDto : INotifyPropertyChanged
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
    }
}
