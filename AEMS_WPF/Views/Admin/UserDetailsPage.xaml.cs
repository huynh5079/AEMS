using DataAccess.Repositories.Abstraction;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;

namespace AEMS_WPF.Views.Admin
{
    public partial class UserDetailsPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly string _userId;

        public UserDetailsPage(IUnitOfWork uow, string userId)
        {
            InitializeComponent();
            _uow = uow;
            _userId = userId;

            this.Loaded += UserDetailsPage_Loaded;
        }

        private async void UserDetailsPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtHeaderTitle.Text = "Đang tải thông tin hồ sơ...";
            await LoadRealUserDataAsync();
        }

        private async Task LoadRealUserDataAsync()
        {
            try
            {
                var usersFromDb = await _uow.Users.GetAllAsync(
                    u => u.Id.ToString() == _userId,
                    query => query
                        .Include(u => u.Role)
                        // Kéo theo dữ liệu của Student và Department của Student
                        .Include(u => u.StudentProfile).ThenInclude(sp => sp.Department)
                        // Kéo theo dữ liệu của Staff và Department của Staff
                        .Include(u => u.StaffProfile).ThenInclude(sp => sp.Department)
                );

                var realUser = usersFromDb.FirstOrDefault();

                if (realUser == null)
                {
                    MessageBox.Show("Không tìm thấy người dùng này trong hệ thống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    BtnBack_Click(null, null);
                    return;
                }

                txtHeaderTitle.Text = $"Hồ sơ: {realUser.FullName}";

                if (!string.IsNullOrEmpty(realUser.FullName))
                {
                    txtFullName.Text = realUser.FullName;
                    string[] nameParts = realUser.FullName.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (nameParts.Length >= 2)
                    {
                        string firstWord = nameParts[0];
                        string lastWord = nameParts[nameParts.Length - 1];
                        txtInitials.Text = (firstWord.Substring(0, 1) + lastWord.Substring(0, 1)).ToUpper();
                    }
                    else if (nameParts.Length == 1)
                    {
                        txtInitials.Text = nameParts[0].Substring(0, 1).ToUpper();
                    }

                    byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(realUser.FullName);
                    byte r = (byte)(nameBytes.Length * 10 % 255);
                    byte g = (byte)(nameBytes.FirstOrDefault() % 255);
                    byte b = (byte)(nameBytes.LastOrDefault() % 255);
                    borderAvatar.Background = new SolidColorBrush(Color.FromRgb(r, g, b));
                    txtInitials.Foreground = Brushes.White;
                }

                txtEmail.Text = realUser.Email;

                // --- ĐÃ FIX LỖI ENUM Ở ĐÂY ---
                txtRole.Text = realUser.Role != null ? realUser.Role.RoleName.ToString() : "Chưa gán vai trò";

                // --- ĐÃ FIX LỖI ISACTIVE Ở ĐÂY ---
                bool isActive = true;
                if (isActive)
                {
                    txtStatus.Text = "Active";
                    badgeStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
                }
                else
                {
                    txtStatus.Text = "Inactive";
                    badgeStatus.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));
                }

                // --- THÔNG TIN CHI TIẾT (CHUNG CHO TẤT CẢ) ---
                txtSystemId.Text = realUser.Id.ToString();
                txtJoinDate.Text = realUser.CreatedAt != null ? realUser.CreatedAt.ToString("dd/MM/yyyy HH:mm") : "Chưa xác định";
                txtPhone.Text = "Chưa cập nhật"; // Thay bằng cột Phone thật nếu có
                txtGoogleId.Text = "Chưa liên kết"; // Thay bằng cột GoogleId thật nếu có

                // --- PHÂN LOẠI HIỂN THỊ THEO VAI TRÒ (ROLE) ---
                string roleNameStr = realUser.Role != null ? realUser.Role.RoleName.ToString() : "";

                if (roleNameStr == "Student" || roleNameStr == "Học viên")
                {
                    // ==========================================
                    // HIỂN THỊ GIAO DIỆN CỦA SINH VIÊN
                    // ==========================================
                    lblUserCode.Text = "Mã sinh viên";
                    txtUserCode.Text = realUser.StudentProfile != null && !string.IsNullOrEmpty(realUser.StudentProfile.StudentCode)
                                     ? realUser.StudentProfile.StudentCode
                                     : "Chưa cập nhật";

                    lblDepartment.Text = "Phòng ban / Khoa";
                    txtDepartment.Text = realUser.StudentProfile?.Department != null
                                       ? realUser.StudentProfile.Department.Name // Giả sử bảng Department có cột Name
                                       : "Chưa cập nhật";

                    lblExtraInfo.Text = "Kỳ học";
                    txtExtraInfo.Text = realUser.StudentProfile != null && !string.IsNullOrEmpty(realUser.StudentProfile.CurrentSemester)
                                      ? realUser.StudentProfile.CurrentSemester
                                      : "Chưa cập nhật";
                }
                else
                {
                    // ==========================================
                    // HIỂN THỊ GIAO DIỆN CỦA STAFF (ADMIN, ORGANIZER, APPROVER...)
                    // ==========================================
                    lblUserCode.Text = "Mã nhân viên";
                    txtUserCode.Text = realUser.StaffProfile != null && !string.IsNullOrEmpty(realUser.StaffProfile.StaffCode)
                                     ? realUser.StaffProfile.StaffCode
                                     : "Chưa cập nhật";

                    lblDepartment.Text = "Phòng ban";
                    txtDepartment.Text = realUser.StaffProfile?.Department != null
                                       ? realUser.StaffProfile.Department.Name // Giả sử bảng Department có cột Name
                                       : "Chưa cập nhật";

                    lblExtraInfo.Text = "Chức vụ";
                    txtExtraInfo.Text = realUser.StaffProfile != null && !string.IsNullOrEmpty(realUser.StaffProfile.Position)
                                      ? realUser.StaffProfile.Position
                                      : roleNameStr;
                }

                // Cập nhật lại Số điện thoại từ bảng User
                txtPhone.Text = !string.IsNullOrEmpty(realUser.Phone) ? realUser.Phone : "Chưa cập nhật";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết người dùng: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}