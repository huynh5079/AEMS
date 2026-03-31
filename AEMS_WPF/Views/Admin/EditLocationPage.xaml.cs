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
    public partial class EditLocationPage : Page
    {
        private readonly IUnitOfWork _uow;
        private string _originalLocationName;
        private string _locationId;
        private Location _editingLocation;

        public EditLocationPage(IUnitOfWork uow, string locationId)
        {
            InitializeComponent();
            _uow = uow;
            _locationId = locationId;
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Khởi tạo ComboBox Type và Status
            cbType.ItemsSource = Enum.GetValues(typeof(LocationTypeEnum)).Cast<LocationTypeEnum>();
            cbStatus.ItemsSource = Enum.GetValues(typeof(LocationStatusEnum)).Cast<LocationStatusEnum>();

            // Tìm Location trong DB bằng Id
            var allLocations = await _uow.Locations.GetAllAsync();
            _editingLocation = allLocations.FirstOrDefault(l => l.Id == _locationId);

            if (_editingLocation != null)
            {
                // Nếu tìm thấy, đổ dữ liệu lên form
                txtName.Text = _editingLocation.Name;
                txtCapacity.Text = _editingLocation.Capacity.ToString();
                txtAddress.Text = _editingLocation.Address;
                txtDescription.Text = _editingLocation.Description;

                cbType.SelectedItem = _editingLocation.Type ?? LocationTypeEnum.Room;
                cbStatus.SelectedItem = _editingLocation.Status;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtAddress.Text))
                {
                    MessageBox.Show("Tên địa điểm và Địa chỉ không được để trống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(txtCapacity.Text, out int capacity) || capacity <= 0)
                {
                    MessageBox.Show("Sức chứa phải là số lớn hơn 0!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Cập nhật lại Object
                _editingLocation.Name = txtName.Text.Trim();
                _editingLocation.Capacity = int.Parse(txtCapacity.Text);
                _editingLocation.Address = txtAddress.Text.Trim();
                _editingLocation.Description = txtDescription.Text.Trim();
                _editingLocation.Type = (LocationTypeEnum)cbType.SelectedItem;
                _editingLocation.Status = (LocationStatusEnum)cbStatus.SelectedItem;
                _editingLocation.UpdatedAt = DateTime.Now;

                // Update và Save (Chuẩn Entity Framework)
                _uow.Locations.UpdateAsync(_editingLocation);
                await _uow.SaveChangesAsync();

                MessageBox.Show("Cập nhật địa điểm thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService.CanGoBack) NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}