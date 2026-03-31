using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AEMS_WPF.Views.Admin
{
    public partial class EditTopicPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly string _oldName; // Dùng Name làm định danh thay vì ID
        private Topic _editingTopic;

        // Constructor nhận Name từ trang ManageTopicsPage truyền sang
        public EditTopicPage(IUnitOfWork uow, string topicName)
        {
            InitializeComponent();
            _uow = uow;
            _oldName = topicName;
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Sử dụng hàm GetAsync từ IGenericRepository để tìm Topic theo Name
                // Biểu thức lambda: t => t.Name == _oldName
                _editingTopic = await _uow.Topics.GetAsync(t => t.Name == _oldName);

                if (_editingTopic != null)
                {
                    // Đổ dữ liệu vào các ô TextBox trên giao diện
                    txtTopicName.Text = _editingTopic.Name;
                    txtTopicDescription.Text = _editingTopic.Description;
                }
                else
                {
                    MessageBox.Show("Không tìm thấy thông tin Topic này!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigateBack();
                }

                Mouse.OverrideCursor = null;
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra dữ liệu đầu vào
            string newName = txtTopicName.Text.Trim();
            string newDesc = txtTopicDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("Tên Topic không được để trống!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // 2. Cập nhật các thông tin mới vào đối tượng đang sửa
                _editingTopic.Name = newName;
                _editingTopic.Description = !string.IsNullOrWhiteSpace(newDesc) ? newDesc : null;

                // BaseEntity của bạn có UpdatedAt kiểu DateTime (không null)
                _editingTopic.UpdatedAt = DateTime.Now;

                // 3. Gọi hàm UpdateAsync theo đúng Interface của nhóm bạn
                await _uow.Topics.UpdateAsync(_editingTopic);

                // 4. Lưu thay đổi xuống Database thật
                await _uow.SaveChangesAsync();

                Mouse.OverrideCursor = null;

                MessageBox.Show("Cập nhật thông tin Topic thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // 5. Quay về trang quản lý
                NavigateBack();
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Lỗi khi cập nhật dữ liệu: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack();
        }

        private void NavigateBack()
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}