using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Organizer.BudgetProposal;
using BusinessLogic.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static BusinessLogic.DTOs.Organizer.BudgetProposal.BugetProposalDtos;
using DataAccess.Enum;

namespace AEMS_WPF.Views.Organizer
{
    public partial class ExpenseSubmissionPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private readonly IBudgetProposalService _budgetService;
        private readonly IFileStorageService _fileStorageService;
        private string? _selectedImagePath;
        private string? _proposalId;

        private ObservableCollection<ExpenseItemViewModel> _expenses = new();

        public ExpenseSubmissionPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = $"Expenses: {eventTitle}";
            _budgetService = App.ServiceProvider.GetRequiredService<IBudgetProposalService>();
            _fileStorageService = App.ServiceProvider.GetRequiredService<IFileStorageService>();

            icExpenses.ItemsSource = _expenses;
            _ = LoadExpenses();
        }

        private async Task LoadExpenses()
        {
            try
            {
                var proposal = await _budgetService.GetByEventAsync(_eventId.ToString());
                if (proposal != null)
                {
                    _proposalId = proposal.ProposalId;
                    var receipts = await _budgetService.GetReceiptsByProposalAsync(_proposalId);
                    
                    _expenses.Clear();
                    foreach (var r in receipts.OrderByDescending(x => x.CreatedAt))
                    {
                        _expenses.Add(new ExpenseItemViewModel(r));
                    }
                    
                    txtNoExpenses.Visibility = _expenses.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    txtNoExpenses.Visibility = Visibility.Visible;
                    txtNoExpenses.Text = "No approved budget proposal found for this event.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading expenses: {ex.Message}");
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedImagePath = openFileDialog.FileName;
                imgReceipt.Source = new BitmapImage(new Uri(_selectedImagePath));
                stackPlaceholder.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_proposalId))
            {
                MessageBox.Show("An approved budget proposal is required to submit expenses.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtExpenseName.Text) || !decimal.TryParse(txtAmount.Text, out decimal amount))
            {
                MessageBox.Show("Please fill in valid expense name and amount.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_selectedImagePath == null)
            {
                MessageBox.Show("Please upload a receipt image as proof.", "AEMS", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSubmit.IsEnabled = false;
            btnSubmit.Content = "Submitting...";

            try
            {
                // 1. Upload proof
                string? cloudUrl = null;
                using (var stream = File.OpenRead(_selectedImagePath))
                {
                    var file = new DummyFormFile(stream, Path.GetFileName(_selectedImagePath));
                    var uploadResult = await _fileStorageService.UploadSingleAsync(file, UploadContext.BudgetEvidence, _user.Id);
                    cloudUrl = uploadResult?.Url;
                }

                if (cloudUrl == null) throw new Exception("Image upload failed.");

                // 2. Submit receipt
                var dto = new CreateExpenseReceiptDto
                {
                    Title = txtExpenseName.Text.Trim(),
                    ActualAmount = amount,
                    ReceiptImageUrl = cloudUrl
                };

                await _budgetService.AddReceiptAsync(_user.Id, _proposalId, dto);
                
                MessageBox.Show("Expense submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reset form
                txtExpenseName.Text = "";
                txtAmount.Text = "";
                _selectedImagePath = null;
                imgReceipt.Source = null;
                stackPlaceholder.Visibility = Visibility.Visible;
                
                await LoadExpenses();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error submitting expense: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSubmit.IsEnabled = true;
                btnSubmit.Content = "Submit for Audit";
            }
        }
    }

    public class ExpenseItemViewModel
    {
        public string Title { get; }
        public decimal ActualAmount { get; }
        public string Status { get; }
        public string StatusColor { get; }
        public DateTime CreatedAt { get; }

        public ExpenseItemViewModel(ExpenseReceiptDto dto)
        {
            Title = dto.Title ?? "Unknown";
            ActualAmount = dto.ActualAmount;
            Status = dto.Status ?? "Pending";
            CreatedAt = dto.CreatedAt;
            
            StatusColor = Status switch
            {
                "Approved" => "#22c55e",
                "Rejected" => "#ef4444",
                _ => "#fbbf24"
            };
        }
    }

    // Quick helper for stream to IFormFile
    internal class DummyFormFile : Microsoft.AspNetCore.Http.IFormFile
    {
        private readonly Stream _stream;
        public DummyFormFile(Stream stream, string fileName)
        {
            _stream = stream;
            FileName = fileName;
            Length = stream.Length;
        }
        public string ContentType => "image/jpeg";
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public Microsoft.AspNetCore.Http.IHeaderDictionary Headers => new Microsoft.AspNetCore.Http.HeaderDictionary();
        public long Length { get; }
        public string Name => "file";
        public string FileName { get; }
        public Stream OpenReadStream() => _stream;
        public void CopyTo(Stream target) => _stream.CopyTo(target);
        public Task CopyToAsync(Stream target, System.Threading.CancellationToken cancellationToken = default) => _stream.CopyToAsync(target, cancellationToken);
    }
}
