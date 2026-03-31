using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AEMS_WPF.Views.Admin
{
    // Lớp để quản lý từng nút bấm phân trang (1, 2, 3...)
    public class PageItem
    {
        public int PageNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class LocationsViewModel : INotifyPropertyChanged
    {
        private int _totalCount;
        private int _availableCount;
        private int _occupiedCount;
        private int _maintenanceCount;
        private int _totalCapacity;
        private int _currentPage = 1;
        private int _totalPages = 1;

        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaginationInfo)); } }
        public int AvailableCount { get => _availableCount; set { _availableCount = value; OnPropertyChanged(); } }
        public int OccupiedCount { get => _occupiedCount; set { _occupiedCount = value; OnPropertyChanged(); } }
        public int MaintenanceCount { get => _maintenanceCount; set { _maintenanceCount = value; OnPropertyChanged(); } }
        public int TotalCapacity { get => _totalCapacity; set { _totalCapacity = value; OnPropertyChanged(); } }

        public int CurrentPage { get => _currentPage; set { _currentPage = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaginationInfo)); } }
        public int TotalPages { get => _totalPages; set { _totalPages = value; OnPropertyChanged(); OnPropertyChanged(nameof(PaginationInfo)); } }

        // CẬP NHẬT LOGIC TEXT CHO GIỐNG HÌNH ẢNH
        public string PaginationInfo
        {
            get
            {
                if (TotalCount == 0) return "Showing 0 to 0 of 0 entries";
                int start = (CurrentPage - 1) * 10 + 1;
                int end = Math.Min(CurrentPage * 10, TotalCount);
                return $"Showing {start} to {end} of {TotalCount} entries";
            }
        }

        // Danh sách các nút phân trang
        public ObservableCollection<PageItem> PageNumbers { get; set; } = new ObservableCollection<PageItem>();

        public void UpdatePagination()
        {
            PageNumbers.Clear();
            for (int i = 1; i <= TotalPages; i++)
            {
                PageNumbers.Add(new PageItem { PageNumber = i, IsActive = i == CurrentPage });
            }
            OnPropertyChanged(nameof(PaginationInfo));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Lớp dữ liệu trung gian hiển thị lên DataGrid
    public class LocationDisplayItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SubName { get; set; }
        public string Address { get; set; }
        public int Capacity { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public partial class ManageLocationsPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly LocationsViewModel _viewModel;
        private List<Location> _allLocationsFromDb = new List<Location>();

        private const int ITEMS_PER_PAGE = 10; // Cố định 10 locations 1 trang

        public ManageLocationsPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;
            _viewModel = new LocationsViewModel();
            this.DataContext = _viewModel;

            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Thiết lập giá trị cho ComboBox Filter Status
            cbFilterStatus.Items.Clear();
            cbFilterStatus.Items.Add("Status (All)"); // Mặc định hiển thị tất cả
            cbFilterStatus.Items.Add("Available");
            cbFilterStatus.Items.Add("Maintenance");
            cbFilterStatus.Items.Add("Occupied");
            cbFilterStatus.Items.Add("Closed");
            cbFilterStatus.SelectedIndex = 0; // Chọn "Status (All)"

            await LoadDataFromDatabaseAsync();
        }

        private async Task LoadDataFromDatabaseAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // 1. Lấy toàn bộ dữ liệu thật từ Database
                var data = await _uow.Locations.GetAllAsync();
                _allLocationsFromDb = data.ToList();

                // 2. Tính toán dữ liệu cho 5 thẻ thống kê (Stat Cards)
                _viewModel.TotalCount = _allLocationsFromDb.Count;
                _viewModel.AvailableCount = _allLocationsFromDb.Count(l => l.Status == LocationStatusEnum.Available);
                _viewModel.OccupiedCount = _allLocationsFromDb.Count(l => l.Status == LocationStatusEnum.Occupied);
                _viewModel.MaintenanceCount = _allLocationsFromDb.Count(l => l.Status == LocationStatusEnum.Maintenance);
                _viewModel.TotalCapacity = _allLocationsFromDb.Sum(l => l.Capacity);

                // 3. Reset về trang 1 và hiển thị dữ liệu
                _viewModel.CurrentPage = 1;
                DisplayCurrentPage();

                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hàm xử lý lọc tìm kiếm và cắt 10 dòng/trang
        private void DisplayCurrentPage()
        {
            string searchText = txtSearch.Text.ToLower().Trim();
            string selectedStatus = cbFilterStatus.SelectedItem?.ToString();

            // Lọc dữ liệu theo Tên/Địa chỉ VÀ Trạng thái
            var filteredList = _allLocationsFromDb
                .Where(l =>
                    // 1. Điều kiện Search Text
                    (string.IsNullOrEmpty(searchText) ||
                     l.Name.ToLower().Contains(searchText) ||
                     (l.Address != null && l.Address.ToLower().Contains(searchText)))
                    &&
                    // 2. Điều kiện Filter Status (Nếu chọn All thì bỏ qua)
                    (selectedStatus == "Status (All)" || l.Status.ToString() == selectedStatus)
                )
                .ToList();

            // Cập nhật thống kê phân trang...
            _viewModel.TotalCount = filteredList.Count;
            _viewModel.TotalPages = (int)Math.Ceiling(filteredList.Count / 10.0);
            if (_viewModel.TotalPages == 0) _viewModel.TotalPages = 1;

            var pagedList = filteredList
    .Skip((_viewModel.CurrentPage - 1) * 10)
    .Take(10)
    .Select(l => new LocationDisplayItem
    {
        Id = l.Id, // <-- Quan trọng: Gắn Id để tìm đúng đối tượng khi click Edit/Delete
        Name = l.Name,
        Address = l.Address ?? "N/A",
        Capacity = l.Capacity,
        Type = l.Type?.ToString() ?? "N/A",
        Status = l.Status.ToString(),
        UpdatedAt = l.UpdatedAt
    }).ToList();

            dgvLocations.ItemsSource = pagedList;
            _viewModel.UpdatePagination();
        }

        // Sự kiện khi bấm thẳng vào một nút số (1, 2, 3,...)
        private void BtnPage_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is PageItem pageItem)
            {
                _viewModel.CurrentPage = pageItem.PageNumber;
                DisplayCurrentPage();
            }
        }

        // ==========================================
        // XỬ LÝ SỰ KIỆN NÚT BẤM (ACTIONS & PAGINATION)
        // ==========================================

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.CurrentPage = 1; // Khi search luôn quay về trang 1
            DisplayCurrentPage();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = string.Empty;
            cbFilterStatus.SelectedIndex = 0; // Reset Combobox về "Status (All)"
            _viewModel.CurrentPage = 1;
            DisplayCurrentPage();
        }

        // Nút chuyển trang: Previous
        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPage > 1)
            {
                _viewModel.CurrentPage--;
                DisplayCurrentPage();
            }
        }

        // Nút chuyển trang: Next
        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CurrentPage < _viewModel.TotalPages)
            {
                _viewModel.CurrentPage++;
                DisplayCurrentPage();
            }
        }

        private void BtnAddLocation_Click(object sender, RoutedEventArgs e)
        {
            // Chuyển hướng sang trang CreateLocationPage, truyền kèm _uow
            NavigationService.Navigate(new CreateLocationPage(_uow));
        }

        // Sự kiện khi nhấn icon ✎ (Bút chì - Chỉnh sửa)
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is LocationDisplayItem item)
            {
                // Truyền Id sang trang Edit
                NavigationService.Navigate(new EditLocationPage(_uow, item.Id));
            }
        }

        // Sự kiện khi nhấn icon 🗑 (Thùng rác - Xóa)
        // Sự kiện khi nhấn icon 🗑 (Thùng rác - Xóa)
        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is LocationDisplayItem item)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa địa điểm '{item.Name}' không?",
                                             "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;

                        // Tìm chính xác Location bằng Id ẩn (Cách chuẩn của AEMS)
                        var allLocations = await _uow.Locations.GetAllAsync();
                        var locationToDelete = allLocations.FirstOrDefault(l => l.Id == item.Id);

                        if (locationToDelete != null)
                        {
                            // Truyền nguyên đối tượng tìm được vào hàm Remove/Delete
                            _uow.Locations.RemoveAsync(locationToDelete);
                            await _uow.SaveChangesAsync();

                            MessageBox.Show("Xóa địa điểm thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadDataFromDatabaseAsync(); // Refresh lại DataGrid
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi Sửa/Xóa (Địa điểm này có thể đang có sự kiện tồn tại).\nLỗi chi tiết: {ex.Message}",
                                        "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}