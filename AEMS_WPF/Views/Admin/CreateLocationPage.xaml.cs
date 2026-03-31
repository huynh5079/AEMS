using DataAccess.Entities;
using DataAccess.Enum;
using DataAccess.Repositories.Abstraction;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace AEMS_WPF.Views.Admin
{
    public partial class CreateLocationPage : Page
    {
        private readonly IUnitOfWork _uow;

        public CreateLocationPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Tải danh sách Type lên ComboBox
            cbType.ItemsSource = Enum.GetValues(typeof(LocationTypeEnum)).Cast<LocationTypeEnum>();
            cbType.SelectedIndex = 0; // Chọn giá trị đầu tiên làm mặc định

            // Tải danh sách Status lên ComboBox (Đã sửa từ RadioButton)
            cbStatus.ItemsSource = Enum.GetValues(typeof(LocationStatusEnum)).Cast<LocationStatusEnum>();
            cbStatus.SelectedIndex = 0; // Mặc định là Available
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Validation (Kiểm tra dữ liệu nhập)
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtAddress.Text))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ Tên địa điểm và Địa chỉ!", "Lỗi Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtCapacity.Text, out int capacity) || capacity <= 0)
                {
                    MessageBox.Show("Sức chứa (Capacity) phải là một số lớn hơn 0!", "Lỗi Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Tạo đối tượng Location mới để lưu vào SQL
                var newLocation = new Location
                {
                    Name = txtName.Text.Trim(),
                    Capacity = capacity,
                    Type = (LocationTypeEnum)cbType.SelectedItem,       // Lấy từ ComboBox Type
                    Status = (LocationStatusEnum)cbStatus.SelectedItem, // Lấy từ ComboBox Status
                    Address = txtAddress.Text.Trim(),
                    Description = txtDescription.Text.Trim(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 3. Lưu vào Database
                await _uow.Locations.CreateAsync(newLocation);
                await _uow.SaveChangesAsync();

                MessageBox.Show("Thêm địa điểm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // 4. Quay lại trang danh sách Location
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra khi lưu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Xử lý nút Hủy
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }

        // Xử lý nút Mũi tên quay lại
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}