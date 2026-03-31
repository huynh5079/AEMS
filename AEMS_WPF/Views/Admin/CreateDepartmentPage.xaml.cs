using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AEMS_WPF.Views.Admin
{
    public partial class CreateDepartmentPage : Page
    {
        private readonly IUnitOfWork _uow;

        public CreateDepartmentPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;
        }

        // ==========================================
        // LOGIC ĐIỀU HƯỚNG
        // ==========================================
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra xem lịch sử điều hướng có trang trước đó không, nếu có thì lùi lại
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            // Nút Hủy bỏ làm nhiệm vụ giống nút Quay lại
            BtnBack_Click(sender, e);
        }

        // ==========================================
        // LOGIC TẠO MỚI PHÒNG BAN
        // ==========================================
        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu từ form
            string deptName = txtDepartmentName.Text.Trim();
            string deptCode = txtDepartmentCode.Text.Trim();

            // 2. Validate dữ liệu
            if (string.IsNullOrEmpty(deptName) || string.IsNullOrEmpty(deptCode))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ Tên và Mã phòng ban.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. Kiểm tra Mã phòng ban đã tồn tại chưa (Sử dụng dữ liệu thật)
            try
            {
                // Gọi DB kiểm tra xem mã này đã có ai dùng chưa
                var existingDepartments = await _uow.Departments.GetAllAsync(d => d.Code == deptCode);

                if (existingDepartments.Any())
                {
                    MessageBox.Show($"Mã phòng ban '{deptCode}' đã tồn tại trong hệ thống. Vui lòng nhập mã khác.", "Lỗi trùng lặp", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtDepartmentCode.Focus();
                    txtDepartmentCode.SelectAll();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối cơ sở dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 4. Đóng gói Entity Department mới
            // (Need namespace: DataAccess.Entities)
            Department newDept = new Department
            {
                Name = deptName, // Lấy Tên
                Code = deptCode, // Lấy Mã
            };

            // 5. Thêm vào Repository
            try
            {
                // Sử dụng hàm CreateAsync như đã quy ước trong team
                await _uow.Departments.CreateAsync(newDept);

                // Lưu thay đổi
                await _uow.SaveChangesAsync();

                // 6. Hiển thị thông báo thành công và quay lại trang danh sách
                MessageBox.Show($"Đã tạo mới phòng ban '{deptName}' thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Tự động quay về danh sách sau khi tạo xong
                BtnBack_Click(null, null);
            }
            catch (Exception ex)
            {
                // Xử lý lỗi khi lưu (ví dụ trùng khóa ngoài)
                MessageBox.Show($"Không thể tạo phòng ban mới: {ex.Message}", "Lỗi lưu dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}