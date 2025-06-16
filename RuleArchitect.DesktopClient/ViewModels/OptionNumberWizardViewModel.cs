// Place in RuleArchitect.DesktopClient/ViewModels/
using System.ComponentModel;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class OptionNumberWizardViewModel : ValidatableViewModel
    {
        private string _optionNumber;
        public string OptionNumber
        {
            get => _optionNumber;
            set
            {
                if (SetProperty(ref _optionNumber, value))
                {
                    Validate();
                }
            }
        }

        public void Validate()
        {
            ClearErrors(nameof(OptionNumber));
            if (string.IsNullOrWhiteSpace(OptionNumber))
            {
                AddError(nameof(OptionNumber), "Option Number cannot be empty.");
            }
        }
    }
}