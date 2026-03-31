using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AEMS_WPF.Views.Admin
{
    public partial class CreateTopicPage : Page
    {
        private readonly IUnitOfWork _uow; // Khai báo UnitOfWork

        // Constructor nhận UnitOfWork được truyền sang
        public CreateTopicPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;
        }

        // ==========================================
        // 1. LOGIC NÚT QUAY LẠI VÀ NÚT CANCEL
        // ==========================================

        // Cả 2 nút này đều thực hiện hành động quay về trang danh sách
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigateBack();
        }

        // Hàm hỗ trợ quay về trang Quản lý
        private void NavigateBack()
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack(); // Quay lại trang ManageTopicsPage
            }
        }

        // ==========================================
        // 2. LOGIC NÚT SAVE CHANGES (TẠO TOPIC MỚI TRONG DB)
        // ==========================================
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string topicName = txtTopicName.Text.Trim();
            string topicDesc = txtTopicDescription.Text.Trim();

            if (string.IsNullOrWhiteSpace(topicName))
            {
                MessageBox.Show("Vui lòng nhập tên Topic!", "Dữ liệu thiếu", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTopicName.Focus();
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait; // Hết lỗi nhờ thư viện Input

                var newTopic = new Topic
                {
                    Name = topicName,
                    Description = !string.IsNullOrWhiteSpace(topicDesc) ? topicDesc : null
                };

                // ĐỔI AddAsync THÀNH Add (Không dùng await ở đây)
                await _uow.Topics.CreateAsync(newTopic);

                // Lưu thực tế xuống DB thì vẫn dùng await bình thường
                await _uow.SaveChangesAsync();

                Mouse.OverrideCursor = null;

                MessageBox.Show($"Đã tạo thành công Topic '{topicName}'!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigateBack();
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Đã xảy ra lỗi khi tạo Topic:\n{ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}