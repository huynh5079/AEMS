using DataAccess.Repositories.Abstraction;
using System.Windows;
using System.Windows.Controls;

namespace AEMS_WPF.Views.Admin
{
    public partial class CreateUserPage : Page
    {
        private readonly IUnitOfWork _uow;

        public CreateUserPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;
        }

        // =====================================
        // SỰ KIỆN NÚT QUAY LẠI
        // =====================================
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        // =====================================
        // SỰ KIỆN NÚT TẠO TÀI KHOẢN (SUBMIT)
        // =====================================
        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy dữ liệu từ Form
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();
            string fullName = txtFullName.Text.Trim();
            string empId = txtEmpId.Text.Trim();
            string position = txtPosition.Text.Trim();

            // Lấy Role Tag
            string selectedRole = (cmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            // 2. Validate sơ bộ
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("Vui lòng điền đầy đủ các thông tin bắt buộc có dấu (*).", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: Tại đây bạn sẽ gọi Entity Framework để insert User vào Database
            // User newUser = new User { Email = email, Password = password, FullName = fullName ... };
            // await _uow.Users.AddAsync(newUser);
            // await _uow.CommitAsync();

            // 3. Thông báo và quay lại
            MessageBox.Show($"Đã tạo tài khoản thành công cho: {fullName}!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}