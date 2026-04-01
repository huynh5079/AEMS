using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using AEMS_WPF.Views.Organizer;
using AEMS_WPF.Views.Common;

namespace AEMS_WPF.Views.Dashboard
{
    public partial class OrganizerDashboardPage : Page
    {
        private readonly LoggedInUserDto _user;

        public OrganizerDashboardPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            
            // Set user profile info
            txtUserName.Text = _user.FullName ?? "Event Organizer";
            txtUserInitial.Text = string.IsNullOrEmpty(_user.FullName) ? "O" : _user.FullName[0].ToString().ToUpper();
            
            // Load default page
            OrganizerFrame.Navigate(new OverviewPage(_user));
        }

        private void BtnNavOverview_Click(object sender, RoutedEventArgs e)
        {
            OrganizerFrame.Navigate(new OverviewPage(_user));
            SetActiveButton(sender as Button);
        }

        private void BtnNavEvents_Click(object sender, RoutedEventArgs e)
        {
            OrganizerFrame.Navigate(new EventListPage(_user));
            SetActiveButton(sender as Button);
        }

        private void BtnNavCreateEvent_Click(object sender, RoutedEventArgs e)
        {
            OrganizerFrame.Navigate(new CreateEventPage(_user));
            SetActiveButton(sender as Button);
        }

        private void BtnNavNotifications_Click(object sender, RoutedEventArgs e)
        {
            OrganizerFrame.Navigate(new NotificationPage(_user));
            SetActiveButton(sender as Button);
        }

        private void BtnNavChat_Click(object sender, RoutedEventArgs e)
        {
            var chat = new ChatWindow(_user);
            chat.Show();
        }

        private void BtnNavActivity_Click(object sender, RoutedEventArgs e)
        {
            OrganizerFrame.Navigate(new ActivityLogPage());
            SetActiveButton(sender as Button);
        }

        private void BtnSignOut_Click(object sender, RoutedEventArgs e)
        {
            var login = new Auth.LoginWindow();
            Window.GetWindow(this).Close();
            login.Show();
        }

        private void SetActiveButton(Button? activeBtn)
        {
            // Simple approach for now to avoid UI tree errors
            if (activeBtn == null) return;
        }
    }
}
