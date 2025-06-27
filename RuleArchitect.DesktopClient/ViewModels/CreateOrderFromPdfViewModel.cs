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

        public CreateOrderFromPdfViewModel(IGenesisOrderGateway orderGateway, IOrderService orderService, INotificationService notificationService)
        {
            _orderGateway = orderGateway;
            _orderService = orderService;
            _notificationService = notificationService;

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
                            LineItems.Add(new ParsedLineItemViewModel(item));
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
            // 1. Filter for only the selected line items
            var selectedItems = LineItems.Where(li => li.IsSelected).ToList();

            if (!selectedItems.Any())
            {
                _notificationService.ShowWarning("No line items were selected.", "Cannot Create Order");
                return;
            }

            // This is where we will eventually map the selected items to SoftwareOptionIds
            // For now, this demonstrates the selection logic. We will complete this in the next step.
            _notificationService.ShowSuccess($"{selectedItems.Count} line items selected. Order creation logic will go here.", "Ready to Create");

            // Example of what will happen next:
            // var createDto = new CreateOrderDto { ... };
            // createDto.SoftwareOptionIds = selectedItems.Select(i => i.MatchedSoftwareOptionId).ToList();
            // await _orderService.CreateOrderAsync(createDto);

            // Close the dialog by passing a "true" result
            DialogHost.CloseDialogCommand.Execute(true, null);
        }
    }
}