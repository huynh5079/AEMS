using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.Service.Event;
using BusinessLogic.Service.Organizer.CheckIn;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Organizer
{
    public partial class TeamManagementPage : Page
    {
        private readonly LoggedInUserDto _user;
        private readonly Guid _eventId;
        private readonly IEventService _eventService;
        private ObservableCollection<EventTeamDto> _teams = new();

        public TeamManagementPage(LoggedInUserDto user, Guid eventId, string eventTitle)
        {
            InitializeComponent();
            _user = user;
            _eventId = eventId;
            txtEventTitle.Text = $"Teams: {eventTitle}";
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            
            icTeams.ItemsSource = _teams;
            LoadTeams();
        }

        private async void LoadTeams()
        {
            try
            {
                var teams = await _eventService.GetEventTeamsAsync(_eventId.ToString());
                _teams.Clear();
                foreach (var team in teams)
                {
                    _teams.Add(team);
                }
                txtTotalTeams.Text = _teams.Count.ToString();
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message}\nInner: {ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"Error loading teams: {msg}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private async void BtnAddTeam_Click(object sender, RoutedEventArgs e)
        {
            // Simple dialog to enter team name
            string teamName = Microsoft.VisualBasic.Interaction.InputBox("Enter Team Name:", "New Team", $"Team {_teams.Count + 1}");
            if (string.IsNullOrWhiteSpace(teamName)) return;

            try
            {
                var success = await _eventService.CreateEventTeamAsync(_eventId.ToString(), teamName, "");
                if (success)
                {
                    LoadTeams();
                }
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException != null ? $"{ex.Message}\nInner: {ex.InnerException.Message}" : ex.Message;
                MessageBox.Show($"Error creating team: {msg}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRandomize_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Team randomization functionality will be available in the next update.", "Coming Soon", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnDeleteTeam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EventTeamDto team)
            {
                var result = MessageBox.Show($"Delete team '{team.TeamName}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _eventService.DeleteEventTeamAsync(team.Id);
                        LoadTeams();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }


        private void BtnAddMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is EventTeamDto team)
            {
                var selectionWindow = new ParticipantSelectionWindow() { Owner = Window.GetWindow(this) };
                if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedUser != null)
                {
                    var user = selectionWindow.SelectedUser;
                    _ = AddMemberToTeam(team.Id, user.id, user.role);
                }
            }
        }

        private async Task AddMemberToTeam(string teamId, string userId, string role)
        {
            try
            {
                if (role == "Student")
                {
                    await _eventService.AddMemberToTeamAsync(teamId, userId, null, "Member");
                }
                else
                {
                    await _eventService.AddMemberToTeamAsync(teamId, null, userId, "Member");
                }
                LoadTeams(); // Refresh
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding member: {ex.Message}");
            }
        }

        private async void BtnRemoveMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is TeamMemberDto member)
            {
                try
                {
                    await _eventService.RemoveMemberFromTeamAsync(member.Id);
                    LoadTeams();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
