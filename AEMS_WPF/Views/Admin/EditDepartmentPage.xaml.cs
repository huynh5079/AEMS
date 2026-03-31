using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AEMS_WPF.Views.Admin
{
    public partial class EditDepartmentPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly string _departmentId;
        private Department _currentDepartment;

        // Hàm khởi tạo nhận vào UOW và ID phòng ban
        public EditDepartmentPage(IUnitOfWork uow, string departmentId)
        {
            InitializeComponent();
            _uow = uow;
            _departmentId = departmentId;

            // Load dữ liệu cũ lên ngay khi trang vừa bật
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Dùng Guid.Parse vì ID lưu trong BaseEntity là dạng Guid
                _currentDepartment = await _uow.Departments.GetByIdAsync(_departmentId);

                if (_currentDepartment != null)
                {
                    // Đổ dữ liệu cũ lên màn hình
                    txtDepartmentName.Text = _currentDepartment.Name;
                    txtDepartmentCode.Text = _currentDepartment.Code;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu phòng ban: {ex.Message}");
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            BtnBack_Click(sender, e);
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDepartment == null) return;

            string newName = txtDepartmentName.Text.Trim();
            string newCode = txtDepartmentCode.Text.Trim();

            // 1. Validate
            if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(newCode))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Tên và Mã phòng ban.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Kiểm tra trùng Mã phòng ban (Chỉ kiểm tra trùng nếu mã bị thay đổi)
                if (_currentDepartment.Code != newCode)
                {
                    var existingDepartments = await _uow.Departments.GetAllAsync(d => d.Code == newCode);
                    if (existingDepartments.Any())
                    {
                        MessageBox.Show($"Mã phòng ban '{newCode}' đã có người sử dụng.", "Lỗi trùng lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // 3. Cập nhật Entity
                _currentDepartment.Name = newName;
                _currentDepartment.Code = newCode;

                // 4. Lưu xuống Database
                await _uow.Departments.UpdateAsync(_currentDepartment);
                await _uow.SaveChangesAsync();

                MessageBox.Show($"Cập nhật phòng ban '{newName}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // 5. Quay về danh sách
                BtnBack_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}