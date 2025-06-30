using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.DTOs.SoftwareOption;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class ParsedLineItemViewModel : BaseViewModel
    {
        private bool _isSelected = true; // Default to selected
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public ParsedLineItemDto ParsedItem { get; }

        // We will add more properties here later for matching rulesheets

        // --- NEW PROPERTIES FOR MATCHING ---
        public ObservableCollection<SoftwareOptionDto> MatchedRulesheets { get; } = new ObservableCollection<SoftwareOptionDto>();

        private SoftwareOptionDto _selectedRulesheet;
        public SoftwareOptionDto SelectedRulesheet
        {
            get => _selectedRulesheet;
            set => SetProperty(ref _selectedRulesheet, value);
        }

        private string _matchStatus;
        public string MatchStatus { get => _matchStatus; private set => SetProperty(ref _matchStatus, value); }

        public ParsedLineItemViewModel(ParsedLineItemDto parsedItem)
        {
            ParsedItem = parsedItem;
            MatchStatus = "Not Matched";
        }

        public async Task FindMatchingRulesheetsAsync(ISoftwareOptionService softwareOptionService)
        {
            var matches = await softwareOptionService.FindSoftwareOptionsByOptionNumberAsync(this.ParsedItem.SoftwareOptionNumber);

            MatchedRulesheets.Clear();
            foreach (var match in matches)
            {
                MatchedRulesheets.Add(match);
            }

            if (matches.Count == 1)
            {
                SelectedRulesheet = matches.First();
                MatchStatus = "Exact Match";
            }
            else if (matches.Count > 1)
            {
                MatchStatus = "Multiple Matches Found";
            }
            else
            {
                MatchStatus = "No Match Found";
            }
        }
    }
}
