using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using Microsoft.Extensions.DependencyInjection;
using DataAccess.Repositories.Abstraction;
using BusinessLogic.Service.System;

namespace AEMS_WPF
{
    public partial class MainWindow : Window
    {
        private readonly LoggedInUserDto _user;

        public MainWindow(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            SetupUIForRole();
        }

        private void SetupUIForRole()
        {
            txtUserName.Text = _user.FullName;
            txtUserRole.Text = _user.Role;
            txtUserInitial.Text = string.IsNullOrEmpty(_user.FullName) ? "?" : _user.FullName[0].ToString().ToUpper();

            
            // Handle Menu Visibility
            if (_user.Role == "Admin")
            {
                BtnUsers.Visibility = Visibility.Visible;
                BtnErrorLogs.Visibility = Visibility.Visible;
                BtnActivityLogs.Visibility = Visibility.Visible;
                BtnApprovals.Visibility = Visibility.Collapsed;
            }
            else if (_user.Role == "Approver")
            {
                BtnApprovals.Visibility = Visibility.Visible;
                BtnEvents.Visibility = Visibility.Collapsed;
                BtnUsers.Visibility = Visibility.Collapsed;
                BtnErrorLogs.Visibility = Visibility.Collapsed;
            }

            if (_user.Role == "Approver")
            {
                // Navigate to a blank page or overview if inside MainWindow, 
                // but usually Approvers should just use the ApproveDashBoard window directly.
                MainFrame.Navigate(new Views.Dashboard.OverviewPage(_user));
            }
            else if (_user.Role == "Organizer")
            {
                MainFrame.Navigate(new Views.Organizer.EventListPage(_user));
            }
            else
            {
                MainFrame.Navigate(new Views.Dashboard.OverviewPage(_user));
            }
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
            {
                switch (btn.Name)
                {
                    case "BtnDashboard":
                        MainFrame.Navigate(new Views.Dashboard.OverviewPage(_user));
                        break;
                    case "BtnEvents":
                        MainFrame.Navigate(new Views.Organizer.EventListPage(_user));
                        break;
                    case "BtnNotifications":
                        MainFrame.Navigate(new Views.Common.NotificationPage(_user));
                        break;
                    case "BtnErrorLogs":
                        var uowErr = App.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        var logSvc = App.ServiceProvider.GetRequiredService<ISystemErrorLogService>();
                        MainFrame.Navigate(new Views.Admin.SystemErrorsPage(uowErr, logSvc));
                        break;
                    case "BtnActivityLogs":
                        MainFrame.Navigate(new Views.Common.ActivityLogPage());
                        break;
                    case "BtnApprovals":
                        var approveWin = new Views.Dashboard.ApproveDashBoard(_user);
                        approveWin.Show();
                        // Optional: this.Close(); if we want to switch shell
                        break;
                    case "BtnUsers":
                        var uow = App.ServiceProvider.GetRequiredService<IUnitOfWork>();
                        MainFrame.Navigate(new Views.Admin.UserManagementPage(uow, _user));
                        break;
                        // Add more cases as more pages are implemented
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new Views.Auth.LoginWindow();
            login.Show();
            this.Close();
        }
    }
}