using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using DataAccess.Repositories.Abstraction;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace AEMS_WPF.Views.Organizer
{
    public partial class ParticipantSelectionWindow : Window
    {
        private readonly IUnitOfWork _unitOfWork;
        public dynamic? SelectedUser { get; private set; }

        public ParticipantSelectionWindow()
        {
            InitializeComponent();
            _unitOfWork = App.ServiceProvider.GetRequiredService<IUnitOfWork>();
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = txtSearch.Text.Trim().ToLower();
            if (search.Length < 1)
            {
                lbParticipants.ItemsSource = null;
                return;
            }

            try
            {
                // To avoid too many queries, we could add a small delay here (debounce)
                // for simplicity in this implementation, we query directly
                var users = await _unitOfWork.Users.GetAllAsync(
                    u => u.DeletedAt == null &&
                         (u.Email.ToLower().Contains(search) || (u.FullName != null && u.FullName.ToLower().Contains(search))),
                    includes: q => q.Include(u => u.Role)
                );

                var result = users
                    .OrderBy(u => u.Email)
                    .Take(10)
                    .Select(u => new
                    {
                        id = u.Id,
                        fullName = u.FullName ?? "N/A",
                        email = u.Email,
                        role = u.Role?.RoleName.ToString() ?? "Unknown"
                    })
                    .ToList();

                lbParticipants.ItemsSource = result;
            }
            catch (Exception ex)
            {
                // Log or handle error
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (lbParticipants.SelectedItem != null)
            {
                SelectedUser = lbParticipants.SelectedItem;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a member.");
            }
        }
    }
}
