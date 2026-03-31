using BusinessLogic.Service.System;
using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AEMS_WPF.Views.Admin
{
    public partial class SystemErrorsPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly ISystemErrorLogService _logService;
        private int _currentPage = 1;   // Trang hiện tại
        private int _pageSize = 10;    // Số dòng trên mỗi trang
        private int _totalPage = 1;    // Tổng số trang (sẽ tính toán sau)
        public SystemErrorsPage(IUnitOfWork uow, ISystemErrorLogService logService)
        {
            InitializeComponent();
            _uow = uow;
            _logService = logService;
            this.Loaded += SystemErrorsPage_Loaded;
        }

        private async void SystemErrorsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private async Task RefreshData(string filterText = "")
        {
            try
            {
                // 1. Hiện con trỏ chờ để người dùng biết hệ thống đang xử lý
                Mouse.OverrideCursor = Cursors.Wait;

                // 2. Lấy dữ liệu gốc từ Database (Sắp xếp mới nhất lên đầu)
                var allLogs = await _uow.SystemErrorLogs.GetAllAsync(
                    filter: null,
                    query => query.OrderByDescending(x => x.CreatedAt)
                );

                // Chuyển về List để thực hiện lọc trên bộ nhớ (In-memory)
                var filteredLogs = allLogs.ToList();

                // 3. LỌC THEO STATUS CODE (Đọc từ ComboBox)
                if (cboStatusCode.SelectedItem is ComboBoxItem selectedItem)
                {
                    string tagValue = selectedItem.Tag?.ToString();

                    // Nếu Tag khác "All", thực hiện lọc theo mã lỗi
                    if (!string.IsNullOrEmpty(tagValue) && tagValue != "All")
                    {
                        if (int.TryParse(tagValue, out int selectedCode))
                        {
                            filteredLogs = filteredLogs.Where(x => x.StatusCode == selectedCode).ToList();
                        }
                    }
                }

                // 4. LỌC THEO TỪ KHÓA TÌM KIẾM (Search Message / Source / User)
                // Chúng ta sử dụng biến filterText truyền từ nút "Lọc Dữ Liệu" vào
                if (!string.IsNullOrEmpty(filterText))
                {
                    var lowerSearch = filterText.Trim().ToLower();
                    filteredLogs = filteredLogs.Where(x =>
                        (x.ExceptionMessage != null && x.ExceptionMessage.ToLower().Contains(lowerSearch)) ||
                        (x.ExceptionType != null && x.ExceptionType.ToLower().Contains(lowerSearch)) ||
                        (x.Source != null && x.Source.ToLower().Contains(lowerSearch)) ||
                        (x.UserId != null && x.UserId.ToLower().Contains(lowerSearch))
                    ).ToList();
                }

                // 5. TÍNH TOÁN TỔNG SỐ TRANG (Sau khi đã lọc xong hết)
                // Công thức: Tổng số dòng / Số dòng mỗi trang (làm tròn lên)
                _totalPage = (int)Math.Ceiling((double)filteredLogs.Count / _pageSize);

                // Đảm bảo nếu tìm kiếm làm giảm số trang, trang hiện tại không bị "văng" ra ngoài
                if (_currentPage > _totalPage && _totalPage > 0) _currentPage = 1;

                // 6. THỰC HIỆN PHÂN TRANG (Paging)
                // Chỉ lấy đúng 10 dòng (hoặc _pageSize) của trang đang chọn để hiển thị
                var pagedData = filteredLogs
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .ToList();

                // 7. CẬP NHẬT GIAO DIỆN
                dgvErrorLogs.ItemsSource = pagedData;

                // Gọi hàm để vẽ lại các nút số trang 1 2 3 ... 47
                UpdatePaginationUI();

                // --- KẾT THÚC PHẦN THÊM MỚI ---
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Trả lại con trỏ chuột bình thường
                Mouse.OverrideCursor = null;
            }
        }
        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {

            // Reset về trang 1 khi bắt đầu tìm kiếm mới
            _currentPage = 1;

            // Lấy nội dung tìm kiếm
            string keyword = txtSearch.Text.Trim();

            // Gọi hàm RefreshData để tải lại dữ liệu theo từ khóa
            await RefreshData(keyword);
        }

        private void BtnViewDetail_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dòng dữ liệu lỗi đang được chọn từ DataGrid
            if (dgvErrorLogs.SelectedItem is SystemErrorLog selectedLog)
            {
                // 2. Khởi tạo cửa sổ Popup mới và truyền dữ liệu lỗi vào
                ErrorLogDetailWindow detailWindow = new ErrorLogDetailWindow(selectedLog);

                // 3. Thiết lập cửa sổ chính làm chủ của Popup (để nó hiện ở giữa Page)
                detailWindow.Owner = Window.GetWindow(this);

                // 4. Hiển thị cửa sổ dưới dạng Modal (người dùng phải đóng Popup mới tương tác tiếp được với Page)
                detailWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một dòng lỗi để xem chi tiết!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnCleanLogs_Click(object sender, RoutedEventArgs e)
        {
            // 1. Hiển thị hộp thoại xác nhận để tránh bấm nhầm
            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa vĩnh viễn các bản ghi lỗi cũ hơn 30 ngày không?",
                "Xác nhận dọn dẹp",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Khi bắt đầu dọn dẹp (Hiện đồng hồ cát)
                    Mouse.OverrideCursor = Cursors.Wait;
                    btnCleanLogs.IsEnabled = false;

                    // 2. Gọi hàm xóa từ Service (mặc định xóa log > 30 ngày)
                    await _logService.DeleteOldLogsAsync(30);

                    // 3. Thông báo thành công
                    MessageBox.Show("Hệ thống đã dọn dẹp các bản ghi lỗi cũ thành công!",
                                    "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    // 4. Tải lại danh sách để cập nhật UI
                    await RefreshData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi dọn dẹp: {ex.Message}", "Lỗi",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Trả lại trạng thái chuột và nút bấm
                    Mouse.OverrideCursor = null;
                    btnCleanLogs.IsEnabled = true;
                }
            }
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem có thể quay lại trang trước đó không
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
        private void txtSearch_GotFocus(object sender, RoutedEventArgs e) {
            txtSearch.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2962FF"));
            txtSearch.BorderThickness = new Thickness(2);
        }

        private void dgvErrorLogs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        // Thêm class phụ để bind dữ liệu cho từng nút số trang
        public class PageItem
        {
            public int PageNumber { get; set; }
            public string Text { get; set; }
            public string BgColor { get; set; } // Xanh nếu là trang hiện tại
            public string ForeColor { get; set; } // Trắng nếu là trang hiện tại
            public bool IsEnabled { get; set; } = true;
        }

        private void UpdatePaginationUI()
        {
            var pageItems = new List<PageItem>();
            if (_totalPage <= 1)
            {
                icPagination.ItemsSource = null;
                return;
            }

            int delta = 2; // Số lượng trang hiển thị quanh trang hiện tại

            for (int i = 1; i <= _totalPage; i++)
            {
                // Điều kiện hiển thị: Trang 1, Trang cuối, hoặc các trang gần trang hiện tại
                if (i == 1 || i == _totalPage || (i >= _currentPage - delta && i <= _currentPage + delta))
                {
                    // Kiểm tra xem có cần chèn dấu "..." phía trước không
                    if (pageItems.Count > 0 && i - pageItems.Last().PageNumber > 1)
                    {
                        pageItems.Add(new PageItem { Text = "...", IsEnabled = false, BgColor = "Transparent", ForeColor = "#9E9E9E" });
                    }

                    pageItems.Add(new PageItem
                    {
                        PageNumber = i,
                        Text = i.ToString(),
                        BgColor = (i == _currentPage) ? "#2962FF" : "Transparent",
                        ForeColor = (i == _currentPage) ? "White" : "#2962FF",
                        IsEnabled = true
                    });
                }
            }

            icPagination.ItemsSource = pageItems;
            txtPageInfo.Text = $"Trang {_currentPage} / {Math.Max(1, _totalPage)}";

            // Cập nhật trạng thái nút Trước/Sau
            btnPrev.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = _currentPage < _totalPage;
        }

        // Hàm xử lý khi bấm vào một con số cụ thể (ví dụ bấm số 5)
        private async void BtnPageNumber_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.DataContext is PageItem clickedPage && clickedPage.IsEnabled)
            {
                _currentPage = clickedPage.PageNumber;
                await RefreshData(txtSearch.Text);
            }
        }
        private async void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await RefreshData(txtSearch.Text);
            }
        }
        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPage)
            {
                _currentPage++;
                await RefreshData(txtSearch.Text);
            }
        }
        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            // Khi người dùng click ra ngoài ô tìm kiếm, đổi màu viền về mặc định
            txtSearch.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFD8DC"));
            txtSearch.BorderThickness = new Thickness(1.5);
        }
        private void UserLink_Click(object sender, RoutedEventArgs e)
        {
            // Lấy nút vừa được bấm
            var button = sender as Button;

            // Lấy dữ liệu của dòng đó (Thay SystemErrorItem bằng Class hiển thị dữ liệu của bạn)
            var errorItem = button?.DataContext as SystemErrorLog; // <--- SỬA TÊN CLASS Ở ĐÂY

            if (errorItem != null && !string.IsNullOrEmpty(errorItem.UserId))
            {
                // Kiểm tra xem dữ liệu có phải là "Thời gia..." hay không. 
                // Nếu chứa chữ "Thời gia" thì return (không làm gì cả).
                if (errorItem.UserId.Contains("Thời gia"))
                {
                    return;
                }

                // Nếu là ID hợp lệ, tiến hành chuyển sang trang UserDetailsPage và truyền ID đi
                if (this.NavigationService != null)
                {
                    // Truyền UnitOfWork và ID của User sang trang chi tiết
                    this.NavigationService.Navigate(new UserDetailsPage(_uow, errorItem.UserId));
                }
            }
        }
    }
}