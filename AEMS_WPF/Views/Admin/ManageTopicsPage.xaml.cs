using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AEMS_WPF.Views.Admin
{
    // =================================================================
    // 1. LỚP VIEWMODEL ĐỂ TRUYỀN DỮ LIỆU THẬT LÊN 4 THẺ THỐNG KÊ (UI)
    // =================================================================
    public class ManageTopicsViewModel : INotifyPropertyChanged
    {
        private int _totalTopicsCount;
        public int TotalTopicsCount { get => _totalTopicsCount; set { _totalTopicsCount = value; OnPropertyChanged(); } }

        private int _addedThisMonthCount;
        public int AddedThisMonthCount { get => _addedThisMonthCount; set { _addedThisMonthCount = value; OnPropertyChanged(); } }

        private int _withDescriptionCount;
        public int WithDescriptionCount { get => _withDescriptionCount; set { _withDescriptionCount = value; OnPropertyChanged(); } }

        private string _lastUpdatedText;
        public string LastUpdatedText { get => _lastUpdatedText; set { _lastUpdatedText = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // =================================================================
    // 2. LỚP ĐỂ HIỂN THỊ DỮ LIỆU TRONG BẢNG (DATAGRID)
    // =================================================================
    public class TopicDisplayItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public partial class ManageTopicsPage : Page
    {
        private readonly IUnitOfWork _uow;
        private ManageTopicsViewModel _viewModel; // Khai báo biến ViewModel

        public ManageTopicsPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;

            // Khởi tạo ViewModel và gán cho giao diện XAML
            _viewModel = new ManageTopicsViewModel();
            this.DataContext = _viewModel;

            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRealDataAsync();
        }

        private async Task LoadRealDataAsync()
        {
            dgvTopics.ItemsSource = null;

            try
            {
                // Gọi lấy toàn bộ dữ liệu Topic từ Database
                var topicsFromDb = await _uow.Topics.GetAllAsync();

                // --------------------------------------------------------
                // PHẦN 1: TÍNH TOÁN DỮ LIỆU THẬT CHO 4 THẺ THỐNG KÊ
                // --------------------------------------------------------

                // Thẻ 1: Tổng số Topic
                _viewModel.TotalTopicsCount = topicsFromDb.Count();

                // Thẻ 2: Số Topic thêm trong tháng này
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                _viewModel.AddedThisMonthCount = topicsFromDb.Count(t => t.CreatedAt.Month == currentMonth && t.CreatedAt.Year == currentYear);

                // Thẻ 3: Số Topic có mô tả đính kèm
                _viewModel.WithDescriptionCount = topicsFromDb.Count(t => !string.IsNullOrWhiteSpace(t.Description));

                // Thẻ 4: Topic được cập nhật gần nhất
                var lastUpdatedTopic = topicsFromDb.OrderByDescending(t => t.UpdatedAt).FirstOrDefault();
                if (lastUpdatedTopic != null)
                {
                    _viewModel.LastUpdatedText = GetTimeAgo(lastUpdatedTopic.UpdatedAt);
                }
                else
                {
                    _viewModel.LastUpdatedText = "N/A";
                }

                // --------------------------------------------------------
                // PHẦN 2: ĐỔ DỮ LIỆU VÀO BẢNG CHÍNH (Đã sắp xếp mới nhất lên đầu)
                // --------------------------------------------------------
                var displayList = topicsFromDb.Select(topic => new TopicDisplayItem
                {
                    Name = topic.Name,
                    Description = !string.IsNullOrWhiteSpace(topic.Description) ? topic.Description : "Chưa có mô tả",
                    CreatedAt = topic.CreatedAt,
                    UpdatedAt = topic.UpdatedAt
                }).OrderByDescending(t => t.CreatedAt).ToList();

                dgvTopics.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =================================================================
        // HÀM HỖ TRỢ: Chuyển đổi thời gian thành dạng "2 hrs ago", "Just now"
        // =================================================================
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            if (timeSpan <= TimeSpan.FromSeconds(60)) return "Just now";
            if (timeSpan <= TimeSpan.FromMinutes(60)) return $"{timeSpan.Minutes} mins ago";
            if (timeSpan <= TimeSpan.FromHours(24)) return $"{timeSpan.Hours} hrs ago";
            if (timeSpan <= TimeSpan.FromDays(30)) return $"{timeSpan.Days} days ago";
            return dateTime.ToString("MMM dd, yyyy"); // Nếu quá 1 tháng thì hiện ngày tháng
        }

        // --- CÁC HÀM XỬ LÝ NÚT BẤM (Giữ nguyên như cũ) ---
        private void BtnAddNew_Click(object sender, RoutedEventArgs e)
        {
            // Chuyển sang trang CreateTopicPage và truyền UnitOfWork sang
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new CreateTopicPage(_uow));
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string keyword = txtSearchTopic.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("Vui lòng nhập từ khóa tìm kiếm!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            MessageBox.Show($"Đang tìm kiếm Topic chứa từ khóa: {keyword}");
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is TopicDisplayItem selectedItem)
                MessageBox.Show($"Xem chi tiết Topic: {selectedItem.Name}");
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is TopicDisplayItem selectedItem)
            {
                // Truyền Name sang trang Edit
                this.NavigationService.Navigate(new EditTopicPage(_uow, selectedItem.Name));
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is TopicDisplayItem selectedItem)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa Topic '{selectedItem.Name}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Mouse.OverrideCursor = Cursors.Wait;

                        // 1. Lấy toàn bộ danh sách để tìm đối tượng thực thể (Entity)
                        var allTopics = await _uow.Topics.GetAllAsync();

                        // 2. Tìm Topic dựa trên Name (Vì bạn không dùng ID)
                        var topicToDelete = allTopics.FirstOrDefault(t => t.Name == selectedItem.Name);

                        if (topicToDelete != null)
                        {
                            // 3. ĐỔI Remove THÀNH DeleteAsync THEO ĐÚNG REPOSITORY CỦA NHÓM BẠN
                            // 3. GỌI ĐÚNG TÊN HÀM: RemoveAsync (Theo Interface bạn gửi)
                            await _uow.Topics.RemoveAsync(topicToDelete);

                            // 4. Lưu thay đổi xuống Database
                            await _uow.SaveChangesAsync();

                            MessageBox.Show("Đã xóa Topic thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                            // 5. Load lại bảng dữ liệu
                            await LoadRealDataAsync();
                        }

                        Mouse.OverrideCursor = null;
                    }
                    catch (Exception ex)
                    {
                        Mouse.OverrideCursor = null;
                        MessageBox.Show($"Lỗi khi xóa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        // --- HÀM NÚT QUAY LẠI DASHBOARD ---
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Tận dụng cơ chế ghi nhớ lịch sử của WPF để lùi lại trang trước đó
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}