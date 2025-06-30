// RuleArchitect.DesktopClient/ViewModels/CreateOrderFromPdfViewModel.cs
using GenesisOrderGateway.Interfaces;
using HeraldKit.Interfaces;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using RuleArchitect.Abstractions.DTOs.Order;
using RuleArchitect.Abstractions.Interfaces;
using RuleArchitect.DesktopClient.Commands;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RuleArchitect.DesktopClient.ViewModels
{
    public class CreateOrderFromPdfViewModel : BaseViewModel
    {
        private readonly IGenesisOrderGateway _orderGateway;
        private readonly IOrderService _orderService;
        private readonly INotificationService _notificationService;
        private readonly ISoftwareOptionService _softwareOptionService;
        private readonly IAuthenticationStateProvider _authStateProvider;

        private bool _isBusy;
        public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }

        private string _pdfFilePath;
        public string PdfFilePath { get => _pdfFilePath; set => SetProperty(ref _pdfFilePath, value); }

        private ParsedOrderDto _parsedOrder;
        public ParsedOrderDto ParsedOrder { get => _parsedOrder; private set => SetProperty(ref _parsedOrder, value); }

        // This will eventually be a collection of a wrapper ViewModel
        public ObservableCollection<ParsedLineItemViewModel> LineItems { get; }

        public ICommand SelectAndParsePdfCommand { get; }
        public ICommand CreateOrderCommand { get; }

        public CreateOrderFromPdfViewModel(IGenesisOrderGateway orderGateway, IOrderService orderService, INotificationService notificationService, ISoftwareOptionService softwareOptionService, IAuthenticationStateProvider authStateProvider)
        {
            _orderGateway = orderGateway;
            _orderService = orderService;
            _notificationService = notificationService;
            _softwareOptionService = softwareOptionService;
            _authStateProvider = authStateProvider;

            LineItems = new ObservableCollection<ParsedLineItemViewModel>();
            SelectAndParsePdfCommand = new RelayCommand(async () => await ExecuteSelectAndParsePdfAsync(), () => !IsBusy);
            // We'll implement the CreateOrder logic in a later step
            CreateOrderCommand = new RelayCommand(async () => await ExecuteCreateOrderAsync(), () => ParsedOrder != null && ParsedOrder.IsSuccess && !IsBusy);
        
        }

        private async Task ExecuteSelectAndParsePdfAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf|All files (*.*)|*.*",
                Title = "Select an Order PDF"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                PdfFilePath = openFileDialog.FileName;
                IsBusy = true;
                LineItems.Clear();
                try
                {
                    ParsedOrder = await _orderGateway.ParseOrderPdfAsync(PdfFilePath);
                    if (ParsedOrder.IsSuccess)
                    {
                        _notificationService.ShowSuccess("PDF parsed successfully!", "Parse Complete");
                        foreach (var item in ParsedOrder.LineItems)
                        {
                            var vm = new ParsedLineItemViewModel(item);
                            LineItems.Add(vm);

                            // --- ADD THIS ---
                            await vm.FindMatchingRulesheetsAsync(_softwareOptionService);
                        }
                    }
                    else
                    {
                        var errors = string.Join("\n", ParsedOrder.ParsingErrors);
                        _notificationService.ShowError($"Could not parse PDF:\n{errors}", "Parse Failed", isCritical: true);
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowError($"An unexpected error occurred: {ex.Message}", "Error", isCritical: true);
                }
                finally
                {
                    IsBusy = false;
                    // Force re-evaluation of command's CanExecute
                    ((RelayCommand)CreateOrderCommand).RaiseCanExecuteChanged();
                }
            }
        }
        private async Task ExecuteCreateOrderAsync()
        {
            var selectedItems = LineItems
                .Where(li => li.IsSelected && li.SelectedRulesheet != null)
                .ToList();

            if (!selectedItems.Any())
            {
                _notificationService.ShowWarning("No line items with a matched and selected rulesheet were chosen.", "Cannot Create Order");
                return;
            }

            // You will need to get the MachineModelId from your UI
            // For now, let's assume it's hardcoded for demonstration
            var machineModelId = 1; // Replace with actual UI selection

            var createDto = new CreateOrderDto
            {
                OrderNumber = ParsedOrder.SalesOrderNumber,
                CustomerName = "Customer from PDF", // You might need to parse this from the PDF as well
                OrderDate = DateTime.UtcNow,
                ControlSystemId = selectedItems.First().SelectedRulesheet.ControlSystemId, // Assumes all have the same
                MachineModelId = machineModelId,
                SoftwareOptionIds = selectedItems.Select(i => i.SelectedRulesheet.SoftwareOptionId).ToList()
            };

            try
            {
                await _orderService.CreateOrderAsync(createDto, _authStateProvider.CurrentUser.UserId);
                _notificationService.ShowSuccess($"Order '{createDto.OrderNumber}' created successfully.", "Order Created");
                DialogHost.CloseDialogCommand.Execute(true, null);
            }
            catch (Exception ex)
            {
                _notificationService.ShowError($"Failed to create order: {ex.Message}", "Creation Error", isCritical: true);
            }
        }
    }
}