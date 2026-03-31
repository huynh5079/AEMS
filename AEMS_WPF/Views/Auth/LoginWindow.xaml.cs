using AEMS_WPF.Views.Admin;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Auth;
using BusinessLogic.Service.System;
using DataAccess.Repositories.Abstraction;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace AEMS_WPF.Views.Auth
{
    public partial class LoginWindow : Window
    {
        private readonly IAuthService _authService;

        public LoginWindow()
        {
            InitializeComponent();
            // Resolve service from App's ServiceProvider
            _authService = App.ServiceProvider.GetRequiredService<IAuthService>();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = Visibility.Collapsed;
            string email = txtEmail.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter both email and password.");
                return;
            }

            try
            {
                // Create DTO as required by IAuthService
                var loginRequest = new LoginRequestDto
                {
                    Email = email,
                    Password = password
                };

                var result = await _authService.LoginAsync(loginRequest);
                
                if (result != null && result.User != null)
                {
                    // Check if role is allowed for Staff App
                    string role = result.User.Role ?? "";
                    if (role == "Admin" || role == "Organizer" || role == "Approver")
                    {
                        // 1. Khởi tạo Cửa sổ khung mới tạo
                        var appWindow = new MainAppWindow();

                        // 2. Phân luồng tùy theo Role
                        if (role == "Admin")
                        {
                            // Lấy các service cần thiết cho Dashboard từ hệ thống
                            var uow = App.ServiceProvider.GetRequiredService<IUnitOfWork>();
                            var logService = App.ServiceProvider.GetRequiredService<ISystemErrorLogService>();

                            // Đẩy trang AdminDashboard vào khung
                            appWindow.RootFrame.Navigate(new AdminDashboardPage(uow, logService));
                        }
                        else if (role == "Approver")
                        {
                            MessageBox.Show("Chào mừng Approver! Giao diện đang được phát triển.");
                            // Sau này đổi thành: appWindow.RootFrame.Navigate(new ApproverDashboardPage());
                        }
                        else if (role == "Organizer")
                        {
                            MessageBox.Show("Chào mừng Organizer! Giao diện đang được phát triển.");
                            // Sau này đổi thành: appWindow.RootFrame.Navigate(new OrganizerDashboardPage());
                        }

                        // 3. Hiển thị cửa sổ chính và Đóng cửa sổ Login
                        appWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        ShowError("Access denied. This application is for Staff only.");
                    }
                }
                else
                {
                    ShowError("Invalid email or password.");
                }
            }
            catch (System.Exception ex)
            {
                // Capture the specific error message from the service
                ShowError(ex.Message);
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visibility = Visibility.Visible;
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
