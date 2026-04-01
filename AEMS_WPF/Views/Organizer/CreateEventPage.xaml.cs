using System.Windows;
using System.Windows.Controls;
using BusinessLogic.Service.Event;
using BusinessLogic.DTOs.Role.Organizer;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.Service.Dashboard;
using DataAccess.Enum;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AEMS_WPF.Views.Organizer
{
    public partial class CreateEventPage : Page
    {
        private readonly IEventService _eventService;
        private readonly IDropdownService _dropdownService;
        private readonly LoggedInUserDto _user;
        private readonly ObservableCollection<CreateAgendaItemDto> _agendas = new();

        public CreateEventPage(LoggedInUserDto user)
        {
            InitializeComponent();
            _user = user;
            _eventService = App.ServiceProvider.GetRequiredService<IEventService>();
            _dropdownService = App.ServiceProvider.GetRequiredService<IDropdownService>();
            
            dgAgenda.ItemsSource = _agendas;
            InitializeTimePickers();
            LoadDropdownsAsync();
            
            // Default dates
            dpStart.SelectedDate = DateTime.Now.AddDays(7);
            cbStartHour.SelectedValue = 8;
            cbStartMin.SelectedValue = 0;

            dpEnd.SelectedDate = DateTime.Now.AddDays(7);
            cbEndHour.SelectedValue = 10;
            cbEndMin.SelectedValue = 0;

            dpRegOpen.SelectedDate = DateTime.Now;
            dpRegClose.SelectedDate = DateTime.Now.AddDays(6);
        }

        private void InitializeTimePickers()
        {
            var hours = Enumerable.Range(0, 24).ToList();
            var minutes = Enumerable.Range(0, 12).Select(i => i * 5).ToList();

            cbStartHour.ItemsSource = hours;
            cbStartMin.ItemsSource = minutes;
            cbEndHour.ItemsSource = hours;
            cbEndMin.ItemsSource = minutes;
        }

        private async void LoadDropdownsAsync()
        {
            try
            {
                var dropdowns = await _dropdownService.GetCreateEventDropdownsAsync();
                cbSemester.ItemsSource = dropdowns.Semesters;
                cbDepartment.ItemsSource = dropdowns.Departments;
                cbTopic.ItemsSource = dropdowns.Topics;
                cbLocation.ItemsSource = dropdowns.Locations;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading dropdowns: {ex.Message}", "AEMS Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void BtnAddSession_Click(object sender, RoutedEventArgs e)
        {
            var startTime = CombineDateTime(dpStart.SelectedDate, cbStartHour.SelectedValue, cbStartMin.SelectedValue);
            _agendas.Add(new CreateAgendaItemDto 
            { 
                SessionName = "New Session", 
                StartTime = startTime,
                EndTime = startTime.AddHours(1)
            });
        }

        private DateTime CombineDateTime(DateTime? date, object? hour, object? minute)
        {
            if (date == null) return DateTime.Now;
            int h = (int)(hour ?? 0);
            int m = (int)(minute ?? 0);
            return new DateTime(date.Value.Year, date.Value.Month, date.Value.Day, h, m, 0);
        }

        private void BtnRemoveSession_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CreateAgendaItemDto item)
            {
                _agendas.Remove(item);
            }
        }

        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtTitle.Text))
                {
                    MessageBox.Show("Please enter an event title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var request = new CreateEventRequestDto
                {
                    Title = txtTitle.Text,
                    Description = txtDescription.Text,
                    StartTime = CombineDateTime(dpStart.SelectedDate, cbStartHour.SelectedValue, cbStartMin.SelectedValue),
                    EndTime = CombineDateTime(dpEnd.SelectedDate, cbEndHour.SelectedValue, cbEndMin.SelectedValue),
                    RegistrationOpenTime = dpRegOpen.SelectedDate ?? DateTime.Now,
                    RegistrationCloseTime = dpRegClose.SelectedDate ?? DateTime.Now,
                    SemesterId = cbSemester.SelectedValue?.ToString() ?? "",
                    DepartmentId = cbDepartment.SelectedValue?.ToString(),
                    TopicId = cbTopic.SelectedValue?.ToString() ?? "",
                    LocationId = cbLocation.SelectedValue?.ToString() ?? "",
                    Capacity = int.TryParse(txtCapacity.Text, out var cap) ? cap : 100,
                    IsDepositRequired = chkIsDeposit.IsChecked ?? false,
                    DepositAmount = decimal.TryParse(txtDepositAmount.Text, out var dep) ? dep : 0,
                    Mode = (EventModeEnum)cbMode.SelectedIndex,
                    Type = (EventTypeEnum)cbType.SelectedIndex,
                    MeetingUrl = txtMeetingUrl.Text,
                    Agendas = _agendas.ToList()
                };

                await _eventService.CreateEventAsync(_user.Id, request);

                MessageBox.Show("Event proposal submitted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Failed to submit event: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
