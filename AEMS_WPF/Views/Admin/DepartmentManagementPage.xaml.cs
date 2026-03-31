using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AEMS_WPF.Views.Admin
{
    // Class Helper chứa dữ liệu 3 cột cho UI
    public class DepartmentDisplayItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public partial class DepartmentManagementPage : Page
    {
        private readonly IUnitOfWork _uow;
        private List<DepartmentDisplayItem> _originalList;

        public DepartmentManagementPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;
            this.Loaded += Page_Loaded;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadRealDataAsync();
        }

        private async Task LoadRealDataAsync()
        {
            try
            {
                var departmentsDb = await _uow.Departments.GetAllAsync();
                _originalList = new List<DepartmentDisplayItem>();

                foreach (var dept in departmentsDb)
                {
                    _originalList.Add(new DepartmentDisplayItem
                    {
                        Id = dept.Id.ToString(),
                        Name = dept.Name ?? "Chưa có tên",
                        Code = !string.IsNullOrEmpty(dept.Code) ? dept.Code : "Chưa cập nhật",
                    });
                }
                icDepartments.ItemsSource = _originalList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu phòng ban: {ex.Message}", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_originalList == null) return;
            string searchText = txtSearch.Text.Trim().ToLower();
            var filtered = _originalList.AsQueryable();

            if (!string.IsNullOrEmpty(searchText))
            {
                filtered = filtered.Where(d =>
                    d.Name.ToLower().Contains(searchText) ||
                    d.Code.ToLower().Contains(searchText)
                );
            }
            icDepartments.ItemsSource = filtered.ToList();
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_originalList == null) return;
            txtSearch.Text = string.Empty;
            icDepartments.ItemsSource = _originalList;
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }

        private void BtnCreateNewDepartment_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null)
            {
                this.NavigationService.Navigate(new CreateDepartmentPage(_uow));
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var departmentItem = button?.DataContext as DepartmentDisplayItem;
            if (departmentItem != null && this.NavigationService != null)
            {
                this.NavigationService.Navigate(new EditDepartmentPage(_uow, departmentItem.Id));
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var departmentItem = button?.DataContext as DepartmentDisplayItem;

            if (departmentItem != null)
            {
                var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa phòng ban '{departmentItem.Name}' không?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        
                        var deptToDelete = await _uow.Departments.GetByIdAsync(departmentItem.Id.ToString());

                        if (deptToDelete != null)   
                        {
                            await _uow.Departments.RemoveAsync(deptToDelete);
                            await _uow.SaveChangesAsync();

                            MessageBox.Show("Đã xóa phòng ban thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                            // Tải lại danh sách
                            await LoadRealDataAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi xóa dữ liệu: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
     
    } 
} 