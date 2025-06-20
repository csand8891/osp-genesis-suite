// RuleArchitect.DesktopClient/ViewModels/SpecCodeViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using RuleArchitect.Abstractions.Interfaces;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class SpecCodeViewModel : BaseViewModel
    {
        private int _originalId;
        private int _specCodeDefinitionId;

        private string _category = string.Empty;
        private string _specCodeNo = string.Empty;
        private string _specCodeBit = string.Empty;
        private string? _description;
        private bool _isDescriptionReadOnly = true;

        private int? _softwareOptionActivationRuleId;
        private string? _activationRuleName;
        private string? _specCodeDisplayName;
        private int? _controlSystemId;
        private bool _isActive = true;

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Action? _itemChangedCallback;

        public SpecCodeViewModel(IServiceScopeFactory scopeFactory, Action? itemChangedCallback = null)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _itemChangedCallback = itemChangedCallback;
            _isDescriptionReadOnly = true;
        }

        public int OriginalId { get => _originalId; set => SetProperty(ref _originalId, value, _itemChangedCallback); }
        public int SpecCodeDefinitionId { get => _specCodeDefinitionId; set => SetProperty(ref _specCodeDefinitionId, value, _itemChangedCallback); }
        public string Category { get => _category; set => SetProperty(ref _category, value, _itemChangedCallback); }
        public string SpecCodeNo { get => _specCodeNo; set => SetProperty(ref _specCodeNo, value, _itemChangedCallback); }
        public string SpecCodeBit { get => _specCodeBit; set => SetProperty(ref _specCodeBit, value, _itemChangedCallback); }
        public string? Description { get => _description; set => SetProperty(ref _description, value, _itemChangedCallback); }
        public int? SoftwareOptionActivationRuleId { get => _softwareOptionActivationRuleId; set => SetProperty(ref _softwareOptionActivationRuleId, value, _itemChangedCallback); }
        public bool IsActive { get => _isActive; set => SetProperty(ref _isActive, value, _itemChangedCallback); }

        public int? ControlSystemId { get => _controlSystemId; set => SetProperty(ref _controlSystemId, value); }
        public bool IsDescriptionReadOnly { get => _isDescriptionReadOnly; set => SetProperty(ref _isDescriptionReadOnly, value); }
        public string? ActivationRuleName { get => _activationRuleName; set => SetProperty(ref _activationRuleName, value); }
        public string? SpecCodeDisplayName { get => _specCodeDisplayName; set => SetProperty(ref _specCodeDisplayName, value); }


        // **UPDATED**: Method signature reverted to accept the scope factory as a parameter.
        public async Task CheckDefinitionAsync(IServiceScopeFactory scopeFactory)
        {
            if (!ControlSystemId.HasValue || ControlSystemId.Value <= 0 ||
                string.IsNullOrWhiteSpace(Category) ||
                string.IsNullOrWhiteSpace(SpecCodeNo) ||
                string.IsNullOrWhiteSpace(SpecCodeBit))
            {
                IsDescriptionReadOnly = false;
                Description = string.Empty;
                SpecCodeDefinitionId = 0;
                return;
            }

            // **UPDATED**: Uses the passed-in scope factory, not the private field.
            using (var scope = scopeFactory.CreateScope())
            {
                var softwareOptionService = scope.ServiceProvider.GetRequiredService<ISoftwareOptionService>();
                var foundSpecCodeDef = await softwareOptionService.FindSpecCodeDefinitionAsync(
                    ControlSystemId.Value, Category, SpecCodeNo, SpecCodeBit);

                if (foundSpecCodeDef != null)
                {
                    Description = foundSpecCodeDef.Description;
                    SpecCodeDefinitionId = foundSpecCodeDef.SpecCodeDefinitionId;
                    IsDescriptionReadOnly = true;
                }
                else
                {
                    IsDescriptionReadOnly = false;
                    SpecCodeDefinitionId = 0;
                }
            }
        }
    }
}
