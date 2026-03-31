using AEMS_WPF.ViewModels;
using AEMS_WPF.Views.Common;
using AEMS_WPF.Views.Admin;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.System;
using DataAccess.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore; 
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.Generic;

namespace AEMS_WPF.Views.Dashboard
{
    public partial class AdminDashboardPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly ISystemErrorLogService _logService;
        private AdminDashboardViewModel _viewModel;
        public SeriesCollection ActivitySeries { get; set; }
        public List<string> DateLabels { get; set; }
        private readonly LoggedInUserDto _user;

        // Tiêm (Inject) IUnitOfWork và ISystemErrorLogService vào Constructor giống hệt Web Controller
        public AdminDashboardPage(IUnitOfWork uow, ISystemErrorLogService logService, LoggedInUserDto user)
        {
            InitializeComponent();
            _uow = uow;
            _logService = logService;
            _user = user;

            // Khởi tạo và gán ViewModel cho DataContext của UI
            _viewModel = new AdminDashboardViewModel();
            this.DataContext = _viewModel;

            // Gọi hàm load dữ liệu không đồng bộ (async)
            _ = LoadDashboardDataAsync();
        }
        private void SystemErrorsCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                // 1. Khởi tạo trang SystemErrorsPage mới 
                // Chúng ta tiêm uow và logService từ Dashboard sang trang chi tiết
                var errorPage = new SystemErrorsPage(_uow, _logService);

                // 2. Thực hiện chuyển trang bằng NavigationService
                if (this.NavigationService != null)
                {
                    this.NavigationService.Navigate(errorPage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể chuyển trang: {ex.Message}", "Lỗi điều hướng", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // 1. Lấy số lượng System Errors trong ngày
                var (dates, counts, todayErrors) = await _logService.GetErrorTrendAsync(7);
                _viewModel.SystemErrorsCount = todayErrors;

                // 2. Lấy dữ liệu User thực tế từ DB
                var allUsers = await _uow.Users.GetAllAsync(null, query => query.Include(u => u.Role));

                // Ô 1: New Users (Tổng user)
                _viewModel.NewUsersCount = allUsers.Count();

                // Ô 2: Đếm Student (Bao quát Enum và String)
                _viewModel.ActiveStudentsCount = allUsers.Count(u => u.Role != null &&
                                                              (u.Role.RoleName == DataAccess.Enum.RoleEnum.Student ||
                                                               u.Role.RoleName.ToString().Contains("Student") ||
                                                               u.Role.RoleName.ToString().Contains("Học viên")));

                // Ô 3: Đếm Staff (Gộp chung Organizer và Approver, bao quát cả Enum và String)
                _viewModel.StaffMemberCount = allUsers.Count(u => u.Role != null &&
                                                      (u.Role.RoleName == DataAccess.Enum.RoleEnum.Organizer ||
                                                       u.Role.RoleName.ToString().Contains("Organizer") ||
                                                       u.Role.RoleName == DataAccess.Enum.RoleEnum.Approver ||
                                                       u.Role.RoleName.ToString().Contains("Approver")));

                // Ô 4: Đếm Admin (Bao quát Enum và String)
                _viewModel.AdminCount = allUsers.Count(u => u.Role != null &&
                                                      (u.Role.RoleName == DataAccess.Enum.RoleEnum.Admin ||
                                                       u.Role.RoleName.ToString().Contains("Admin")));

                // Tính toán % cho bảng User Category bên dưới
                if (_viewModel.NewUsersCount > 0)
                {
                    _viewModel.StudentPercentage = Math.Round((double)_viewModel.ActiveStudentsCount / _viewModel.NewUsersCount * 100, 1);
                    _viewModel.StaffPercentage = Math.Round((double)_viewModel.StaffMemberCount / _viewModel.NewUsersCount * 100, 1);
                    _viewModel.AdminPercentage = Math.Round((double)_viewModel.AdminCount / _viewModel.NewUsersCount * 100, 1);
                }
                chartUserCategory.Series = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Student",
                        Values = new ChartValues<int> { _viewModel.ActiveStudentsCount },
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181C32")), // Màu Đen nhám
                        DataLabels = false // Tắt nhãn chữ trên hình, chỉ hiện khi Hover
                    },
                    new PieSeries
                    {
                        Title = "Staff",
                        Values = new ChartValues<int> { _viewModel.StaffMemberCount },
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#009EF7")), // Xanh Dương
                        DataLabels = false
                    },
                    new PieSeries
                    {
                        Title = "Admin",
                        Values = new ChartValues<int> { _viewModel.AdminCount }, // Chắc chắn là _viewModel.AdminCount
                        Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#50CD89")), // Xanh Lá
                        DataLabels = false
                    }
                };

                var allEvents = await _uow.Events.GetAllAsync(null, query => query.Include(e => e.Department));

                // Sau khi lấy được dữ liệu, thực hiện sắp xếp giảm dần theo ngày tạo (hoặc ngày diễn ra) và lấy 5 dòng đầu
                var topRecentEvents = allEvents
                    .OrderByDescending(e => e.CreatedAt)//Ghi chú: Đổi "CreatedDate" thành "StartDate" nếu bạn muốn xếp theo ngày diễn ra
                    .Take(5)
                    .ToList();

                // Gán danh sách này vào ViewModel để DataGrid/ListView ở XAML tự động nhận dữ liệu 
                _viewModel.RecentEvents = topRecentEvents;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load dashboard data: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewUsersCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (this.NavigationService != null)
            {
                // Chuyển sang trang Quản lý người dùng
                this.NavigationService.Navigate(new UserManagementPage(_uow, _user));
            }
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e)
        {
            // 1. Khởi tạo và hiển thị lại màn hình Đăng nhập ngay lập tức
            var loginWindow = new AEMS_WPF.Views.Auth.LoginWindow();
            loginWindow.Show();

            // 2. Tìm Cửa sổ đang chứa trang Dashboard này và đóng nó lại
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }
        private void BtnNavEvents_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý hiệu ứng Accordion cho menu "Events Data"
            if (SubMenuEventsData.Visibility == Visibility.Collapsed)
            {
                // Mở Menu
                SubMenuEventsData.Visibility = Visibility.Visible;
                TxtEventsArrow.Text = "▲";
                TxtEventsData.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0052CC"));
                TxtEventsArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0052CC"));
            }
            else
            {
                // Đóng Menu
                SubMenuEventsData.Visibility = Visibility.Collapsed;
                TxtEventsArrow.Text = "▼";
                TxtEventsData.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9AA4B2"));
                TxtEventsArrow.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9AA4B2"));
            }
        }
        private void BtnNavDashboard_Click(object sender, RoutedEventArgs e)
        {
            // Khi đang ở Dashboard mà bấm nút Overview (Dashboard) thì tải lại trang
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new AdminDashboardPage(_uow, _logService, _user));
            }
        }
        private void MenuDepartment_Click(object sender, RoutedEventArgs e)
        {
            // Dùng NavigationService có sẵn của Page để chuyển trang
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new DepartmentManagementPage(_uow));
            }
        }
        // ==========================================
        // KHỐI CODE XỬ LÝ BIỂU ĐỒ LIVECHARTS
        // ==========================================

        // 1. Sự kiện khi đổi tùy chọn Last 7 Days / Last 30 Days
        private async void cboTimeRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboTimeRange.SelectedItem is ComboBoxItem selectedItem && chartActivity != null)
            {
                int days = int.Parse(selectedItem.Tag.ToString());
                await LoadRealChartDataAsync(days);
            }
        }

        // 2. Logic gọi dữ liệu từ _logService và vẽ biểu đồ 1 đường
        private async Task LoadRealChartDataAsync(int days)
        {
            try
            {
                // Gọi hàm có sẵn của bạn để lấy dữ liệu cực nhanh
                var (dates, counts, todayErrors) = await _logService.GetErrorTrendAsync(days);

                // Cập nhật trục X (Ngày)
                DateLabels = dates;
                axisX.Labels = DateLabels;

                // Cấu hình biểu đồ
                var chartValues = new ChartValues<int>(counts);

                ActivitySeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "System Errors",
                        Values = chartValues,
                        LineSmoothness = 1, // Làm cong mượt
                        Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")), // Màu xanh
                        StrokeThickness = 3,
                        Fill = Brushes.Transparent, // ĐỂ TRONG SUỐT ĐỂ HIỂN THỊ ĐÚNG 1 ĐƯỜNG KẺ
                        PointGeometrySize = 12, // Dấu chấm tròn to
                        PointForeground = Brushes.White,
                    }
                };

                // Đổ lên giao diện
                chartActivity.Series = ActivitySeries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi vẽ biểu đồ: {ex.Message}");
            }
        }

        // 3. Sự kiện khi bấm vào các dấu chấm tròn trên biểu đồ
        private void Chart_OnDataClick(object sender, ChartPoint chartPoint)
        {
            if (this.NavigationService != null)
            {
                // Chuyển sang trang SystemErrorLogs và truyền đủ tham số
                this.NavigationService.Navigate(new SystemErrorsPage(_uow, _logService));
            }
        }
        // ==========================================
        private void BtnNavTopics_Click(object sender, RoutedEventArgs e)
        {
            // Dùng NavigationService có sẵn của Page để chuyển sang trang ManageTopicsPage
            if (this.NavigationService != null)
            {
                // Tiêm UnitOfWork sang trang mới để lấy dữ liệu thật
                this.NavigationService.Navigate(new ManageTopicsPage(_uow));
            }
        }
        private void BtnNavLocations_Click(object sender, RoutedEventArgs e) {
            if (this.NavigationService != null)
            {
                // Tiêm UnitOfWork sang trang mới để lấy dữ liệu thật
                this.NavigationService.Navigate(new ManageLocationsPage(_uow));
            }
        }
        private void BtnNavCommunity_Click(object sender, RoutedEventArgs e) 
        {
            if (this.NavigationService != null)
            {
                // Chuyển sang trang Quản lý người dùng
                this.NavigationService.Navigate(new UserManagementPage(_uow, _user));
            }
        }
        private void BtnNavReports_Click(object sender, RoutedEventArgs e) { }
        private void BtnNavFeedback_Click(object sender, RoutedEventArgs e) {
            if (this.NavigationService != null)
            {
                // Chuyển sang trang Quản lý người dùng
                this.NavigationService.Navigate(new AdminFeedbackPage(_uow));
            }
        }
        private void BtnNavSettings_Click(object sender, RoutedEventArgs e) { }
        private void BtnNavSecurity_Click(object sender, RoutedEventArgs e) { }

        private void BtnNavNotifications_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new NotificationPage(_user));
            }
        }

        private void BtnNavActivityLog_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new ActivityLogPage());
            }
        }

        private void BtnNavErrorLog_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new SystemErrorsPage(_uow, _logService));
            }
        }
    }
}