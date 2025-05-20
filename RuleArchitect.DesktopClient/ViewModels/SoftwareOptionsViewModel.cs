// In RuleArchitect.DesktopClient/ViewModels/SoftwareOptionsViewModel.cs
using RuleArchitect.ApplicationLogic.DTOs; // Or your domain models
using RuleArchitect.ApplicationLogic.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
// using YourProject.Commands; // For RelayCommand
// using YourProject.Models; // For a BaseViewModel if you have one

public class SoftwareOptionsViewModel : INotifyPropertyChanged // Or your BaseViewModel
{
    private readonly ISoftwareOptionService _softwareOptionService;

    public ObservableCollection<SoftwareOptionDto> SoftwareOptions { get; private set; }
    // ... other properties and commands ...

    public ICommand LoadCommand { get; }

    public SoftwareOptionsViewModel(ISoftwareOptionService softwareOptionService)
    {
        _softwareOptionService = softwareOptionService;
        SoftwareOptions = new ObservableCollection<SoftwareOptionDto>();
        LoadCommand = new RelayCommand(async () => await LoadSoftwareOptionsAsync());
        // ... initialize other commands ...
    }

    private async Task LoadSoftwareOptionsAsync()
    {
        var options = await _softwareOptionService.GetAllSoftwareOptionsAsync();
        SoftwareOptions.Clear();
        foreach (var option in options)
        {
            SoftwareOptions.Add(option);