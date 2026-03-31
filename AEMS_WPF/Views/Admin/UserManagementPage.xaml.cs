using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace AEMS_WPF.Views.Admin
{
    /// <summary>
    /// Helper class để chứa dữ liệu đã được chế biến cho UI
    /// Giúp việc lọc dữ liệu bằng LINQ sạch sẽ hơn dynamic
    /// </summary>
    public class UserDisplayItem
    {
        public string Id { get; set; }
        public string Initials { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarBgColor { get; set; }
        public string RoleName { get; set; } // Tên vai trò kiểu chuỗi (Admin, Student,...)
        public string RoleBgColor { get; set; }
        public string RoleTextColor { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; } // Chữ hiển thị: Hoạt động/Đã khóa
        public string StatusColor { get; set; }

        // Thuộc tính ẩn: Dùng để lọc trạng thái hoạt động (bool)
        public bool IsActive { get; set; }
        public string RawStatus { get; set; }
    }

    public partial class UserManagementPage : Page
    {
        private readonly IUnitOfWork _uow;

        // Nơi lưu trữ danh sách gốc chứa toàn bộ user đã load từ DB
        private List<UserDisplayItem> _originalUserDisplayList;

        public UserManagementPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;

            // Gọi hàm lấy dữ liệu thật từ Database khi trang mở
            _ = LoadRealDataAsync();
        }

        /// <summary>
        /// Hàm load dữ liệu gốc từ Database, chế biến cho UI và lưu vào list gốc
        /// </summary>
        private async Task LoadRealDataAsync()
        {
            try
            {
                // 1. Lấy toàn bộ User từ Database (nhớ Include Role)
                var usersFromDb = await _uow.Users.GetAllAsync(null, query => query.Include(u => u.Role));

                _originalUserDisplayList = new List<UserDisplayItem>();

                var avatarColors = new[] { "#F59E0B", "#0D9488", "#8B5CF6", "#EF4444", "#3B82F6", "#10B981" };
                int colorIndex = 0;

                // 2. Duyệt qua từng User và "chế biến" dữ liệu cho UI
                foreach (var u in usersFromDb)
                {
                    // --- XỬ LÝ CHỮ CÁI ĐẦU (INITIALS) CHO AVATAR ---
                    string initials = "U";
                    if (!string.IsNullOrEmpty(u.FullName))
                    {
                        var nameParts = u.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length >= 2) initials = $"{nameParts[0][0]}{nameParts[^1][0]}".ToUpper();
                        else initials = nameParts[0].Substring(0, 1).ToUpper();
                    }

                    // --- XỬ LÝ VAI TRÒ (ROLE) VÀ MÀU SẮC ---
                    // Giữ nguyên RoleName gốc để lọc
                    string rawRoleName = u.Role != null ? u.Role.RoleName.ToString() : "UNKNOWN";
                    

                    string roleBgColor = "#F1F5F9"; // Màu xám mặc định
                    string roleTextColor = "#64748B";

                    // Chuyển sang uppercase chỉ để hiển thị trên UI
                    string roleDisplayName = rawRoleName.ToUpper();

                    if (roleDisplayName == "ADMIN")
                    { roleBgColor = "#FEE2E2"; roleTextColor = "#EF4444"; } // Đỏ
                    else if (roleDisplayName == "STUDENT")
                    { roleBgColor = "#DCFCE7"; roleTextColor = "#16A34A"; } // Xanh lá
                    else if (roleDisplayName == "ORGANIZER" || roleDisplayName == "APPROVER")
                    { roleBgColor = "#DBEAFE"; roleTextColor = "#2563EB"; } // Xanh dương

                    // --- XỬ LÝ TRẠNG THÁI (STATUS) MỚI ---
                    // LƯU Ý: Thay bằng biến lưu status thực tế từ Database của bạn (ví dụ: u.Status)
                    string rawStatus = "Active"; // Giả định

                    // Set màu tùy theo trạng thái
                    string statusColor = "#16A34A"; // Mặc định xanh lá (Active)
                    if (rawStatus == "Inactive") statusColor = "#94A3B8"; // Xám
                    else if (rawStatus == "Banned") statusColor = "#EF4444"; // Đỏ
                    else if (rawStatus == "Pending") statusColor = "#F59E0B"; // Vàng
                    bool isActive = true;

                    

                    // --- ĐÓNG GÓI VÀO DANH SÁCH HELPER CLASS ---
                    _originalUserDisplayList.Add(new UserDisplayItem
                    {
                        Id = u.Id.ToString(),
                        Initials = initials,
                        FullName = u.FullName ?? "Người dùng ẩn danh",
                        Email = u.Email ?? "Chưa cập nhật",
                        AvatarBgColor = avatarColors[colorIndex % avatarColors.Length],
                        RoleName = rawRoleName, // Lưu raw để lọc
                        RoleBgColor = roleBgColor,
                        RoleTextColor = roleTextColor,
                        // LƯU Ý: Thay 'u.CreatedAt' bằng cột ngày tạo thật
                        CreatedAt = u.CreatedAt != null ? u.CreatedAt.ToString("dd/MM/yyyy") : "N/A",
                        Status = rawStatus, // Hiển thị nguyên chữ tiếng Anh (Active, Banned...) ra UI
                        StatusColor = statusColor,
                        RawStatus = rawStatus // Lưu lại để lọc
                    });
                    colorIndex++;
                }

                // 3. Hiển thị danh sách gốc lên giao diện ban đầu
                icUsers.ItemsSource = _originalUserDisplayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu người dùng: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================================================
        // logic BỘ LỌC VÀ TÌM KIẾM
        // =========================================================

        /// <summary>
        /// Sự kiện Click nút "Lọc kết quả"
        /// Lấy giá trị từ các ô nhập liệu và lọc trên danh sách gốc
        /// </summary>
        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_originalUserDisplayList == null) return;

            // 1. Lấy giá trị từ các ô bộ lọc
            string searchText = txtSearch.Text.Trim().ToLower(); // Tìm tên/email (viết thường để so sánh)

            // Lấy thuộc tính Tag từ ComboBoxItem được chọn
            string selectedRoleTag = (cmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            string selectedStatusTag = (cmbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            // 2. Sử dụng LINQ trên AsQueryable để xây dựng bộ lọc
            // Chúng ta lọc trên danh sách helper class sạch sẽ.
            var filteredResult = _originalUserDisplayList.AsQueryable();

            // Lọc 1: Theo ô Tìm kiếm (Tên hoặc Email)
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredResult = filteredResult.Where(u =>
                    u.FullName.ToLower().Contains(searchText) ||
                    u.Email.ToLower().Contains(searchText));
            }

            // Lọc 2: Theo Vai trò (Lọc bằng string-comparison không phân biệt hoa thường)
            if (selectedRoleTag != "All")
            {
                filteredResult = filteredResult.Where(u => u.RoleName.Equals(selectedRoleTag, StringComparison.OrdinalIgnoreCase));
            }

            // Lọc 3: Theo Trạng thái (Lọc bằng thuộc tính bool ẩn)
            // Lọc 3: Theo Trạng thái
            if (selectedStatusTag != "All")
            {
                // So sánh chuỗi RawStatus với Tag của ComboBox
                filteredResult = filteredResult.Where(u => u.RawStatus.Equals(selectedStatusTag, StringComparison.OrdinalIgnoreCase));
            }

            // 3. Hiển thị kết quả đã lọc ra giao diện
            icUsers.ItemsSource = filteredResult.ToList();
        }

        /// <summary>
        /// Sự kiện Click nút dấu X (Hủy bộ lọc)
        /// Xóa trống các ô nhập liệu và hiển thị lại toàn bộ user
        /// </summary>
        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_originalUserDisplayList == null) return;

            // 1. Reset các ô bộ lọc về trạng thái ban đầu
            txtSearch.Text = string.Empty;
            cmbRole.SelectedIndex = 0; // "Tất cả vai trò"
            cmbStatus.SelectedIndex = 0; // "Tất cả trạng thái"

            // 2. Hiển thị lại danh sách gốc toàn bộ User
            icUsers.ItemsSource = _originalUserDisplayList;
        }

        // --- SỰ KIỆN NÚT QUAY LẠI (GIỮ NGUYÊN) ---
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
        // Sự kiện khi bấm nút "Tạo Tài Khoản Mới"
        private void BtnCreateNewUser_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem NavigationService có tồn tại không rồi thực hiện chuyển trang
            if (this.NavigationService != null)
            {
                // Khởi tạo trang CreateUserPage và truyền _uow vào (nếu trang đó cần tương tác DB)
                this.NavigationService.Navigate(new CreateUserPage(_uow));
            }
        }
        // Sự kiện khi bấm nút 👁 Xem chi tiết
        private void BtnViewUserDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            // Ép kiểu về class UserDisplayItem đã tạo ở trên
            var selectedUser = button?.DataContext as UserDisplayItem;

            if (selectedUser != null && !string.IsNullOrEmpty(selectedUser.Id) && this.NavigationService != null)
            {
                // Truyền thẳng chuỗi ID thật sang trang Chi tiết
                this.NavigationService.Navigate(new UserDetailsPage(_uow, selectedUser.Id));
            }
        }
    }
}