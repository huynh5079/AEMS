using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using BusinessLogic.DTOs.Authentication.Login;

namespace AEMS_WPF.Views.Admin
{
    public class UserDisplayItem
    {
        public string Id { get; set; }
        public string Initials { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string AvatarBgColor { get; set; }
        public string RoleName { get; set; }
        public string RoleBgColor { get; set; }
        public string RoleTextColor { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string RawStatus { get; set; }
        public bool IsActive { get; set; }
    }

    public partial class UserManagementPage : Page
    {
        private readonly IUnitOfWork _uow;
        private readonly LoggedInUserDto _currentUser;
        private List<UserDisplayItem> _originalUserDisplayList;

        public UserManagementPage(IUnitOfWork uow, LoggedInUserDto currentUser)
        {
            InitializeComponent();
            _uow = uow;
            _currentUser = currentUser;
            _ = LoadRealDataAsync();
        }

        private async Task LoadRealDataAsync()
        {
            try
            {
                var usersFromDb = await _uow.Users.GetAllAsync(null, query => query.Include(u => u.Role));
                _originalUserDisplayList = new List<UserDisplayItem>();

                var avatarColors = new[] { "#F59E0B", "#0D9488", "#8B5CF6", "#EF4444", "#3B82F6", "#10B981" };
                int colorIndex = 0;

                foreach (var u in usersFromDb)
                {
                    string initials = "U";
                    if (!string.IsNullOrEmpty(u.FullName))
                    {
                        var nameParts = u.FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length >= 2) initials = $"{nameParts[0][0]}{nameParts[^1][0]}".ToUpper();
                        else if (nameParts.Length > 0) initials = nameParts[0].Substring(0, 1).ToUpper();
                    }

                    string rawRoleName = u.Role != null ? u.Role.RoleName.ToString() : "Student";
                    string roleBgColor = "#334155";
                    string roleTextColor = "#94A3B8";

                    if (rawRoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase)) { roleBgColor = "#7f1d1d"; roleTextColor = "#f87171"; }
                    else if (rawRoleName.Equals("Student", StringComparison.OrdinalIgnoreCase)) { roleBgColor = "#064e3b"; roleTextColor = "#34d399"; }
                    else if (rawRoleName.Equals("Organizer", StringComparison.OrdinalIgnoreCase) || rawRoleName.Equals("Approver", StringComparison.OrdinalIgnoreCase)) { roleBgColor = "#1e3a8a"; roleTextColor = "#60a5fa"; }

                    string rawStatus = u.Status.ToString();
                    string statusColor = "#10B981";
                    if (rawStatus == "InActive" || rawStatus == "Banned") statusColor = "#EF4444";
                    else if (rawStatus == "Pending") statusColor = "#F59E0B";

                    _originalUserDisplayList.Add(new UserDisplayItem
                    {
                        Id = u.Id,
                        Initials = initials,
                        FullName = u.FullName ?? "Anonymous",
                        Email = u.Email ?? "No Email",
                        AvatarBgColor = avatarColors[colorIndex % avatarColors.Length],
                        RoleName = rawRoleName,
                        RoleBgColor = roleBgColor,
                        RoleTextColor = roleTextColor,
                        CreatedAt = u.CreatedAt.ToString("dd/MM/yyyy"),
                        Status = rawStatus,
                        StatusColor = statusColor,
                        RawStatus = rawStatus,
                        IsActive = rawStatus == "Active"
                    });
                    colorIndex++;
                }

                icUsers.ItemsSource = _originalUserDisplayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load users: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_originalUserDisplayList == null) return;

            string searchText = txtSearch.Text.Trim().ToLower();
            string selectedRoleTag = (cmbRole.SelectedItem as ComboBoxItem)?.Tag?.ToString();
            string selectedStatusTag = (cmbStatus.SelectedItem as ComboBoxItem)?.Tag?.ToString();

            var filteredResult = _originalUserDisplayList.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredResult = filteredResult.Where(u => u.FullName.ToLower().Contains(searchText) || u.Email.ToLower().Contains(searchText));
            }

            if (selectedRoleTag != "All")
            {
                filteredResult = filteredResult.Where(u => u.RoleName.Equals(selectedRoleTag, StringComparison.OrdinalIgnoreCase));
            }

            if (selectedStatusTag != "All")
            {
                filteredResult = filteredResult.Where(u => u.RawStatus.Equals(selectedStatusTag, StringComparison.OrdinalIgnoreCase));
            }

            icUsers.ItemsSource = filteredResult.ToList();
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_originalUserDisplayList == null) return;
            txtSearch.Text = string.Empty;
            cmbRole.SelectedIndex = 0;
            cmbStatus.SelectedIndex = 0;
            icUsers.ItemsSource = _originalUserDisplayList;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }

        private void BtnCreateNewUser_Click(object sender, RoutedEventArgs e)
        {
            // NavigationService.Navigate(new CreateUserPage(_uow));
        }

        private void BtnViewUserDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UserDisplayItem user)
            {
                this.NavigationService.Navigate(new UserDetailsPage(_uow, user.Id));
            }
        }

        private void BtnMessage_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UserDisplayItem user)
            {
                var chatWindow = new Common.ChatWindow(_currentUser, user.Id);
                chatWindow.Show();
            }
        }

        private async void BtnToggleBan_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is UserDisplayItem userItem)
            {
                string action = userItem.Status == "Active" ? "Ban" : "Unban";
                var confirm = MessageBox.Show($"Are you sure you want to {action} {userItem.FullName}?",
                    "Confirm Action", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    try
                    {
                        var user = await _uow.Users.GetByIdAsync(userItem.Id);
                        if (user != null)
                        {
                            if (userItem.Status == "Active")
                            {
                                user.Status = DataAccess.Enum.UserStatusEnum.Banned;
                                user.IsBanned = true;
                            }
                            else
                            {
                                user.Status = DataAccess.Enum.UserStatusEnum.Active;
                                user.IsBanned = false;
                            }

                            await _uow.Users.UpdateAsync(user);
                            await _uow.SaveChangesAsync();

                            MessageBox.Show($"User successfully {action}ned.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            await LoadRealDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Operation failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
