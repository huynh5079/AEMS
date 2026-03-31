using DataAccess.Entities;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AEMS_WPF.ViewModels
{
    public class AdminDashboardViewModel : INotifyPropertyChanged
    {
        private int _systemErrorsCount;
        public int SystemErrorsCount
        {
            get => _systemErrorsCount;
            set { _systemErrorsCount = value; OnPropertyChanged(); }
        }

        private int _newUsersCount;
        public int NewUsersCount
        {
            get => _newUsersCount;
            set { _newUsersCount = value; OnPropertyChanged(); }
        }

        private int _activeStudentsCount;
        public int ActiveStudentsCount
        {
            get => _activeStudentsCount;
            set { _activeStudentsCount = value; OnPropertyChanged(); }
        }

        private int _staffMemberCount;
        public int StaffMemberCount
        {
            get => _staffMemberCount;
            set { _staffMemberCount = value; OnPropertyChanged(); }
        }

        // Tỷ lệ % cho User Category (Tuỳ chọn để UI đẹp hơn)
        private double _studentPercentage;
        public double StudentPercentage
        {
            get => _studentPercentage;
            set { _studentPercentage = value; OnPropertyChanged(); }
        }

        private double _staffPercentage;
        public double StaffPercentage
        {
            get => _staffPercentage;
            set { _staffPercentage = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class EventItemDto
        {
            public string EventName { get; set; }
            public string DepartmentName { get; set; }
            public int Capacity { get; set; }
            public string Status { get; set; }
            public string StatusBgColor { get; set; }
            public string StatusTextColor { get; set; }
            public string DateTimeString { get; set; }
        }
        private IEnumerable<Event> _recentEvents; // Thay 'Event' bằng tên model thực tế của bạn
        public IEnumerable<Event> RecentEvents
        {
            get => _recentEvents;
            set
            {
                _recentEvents = value;
                OnPropertyChanged(nameof(RecentEvents)); // Gọi hàm để báo cho UI cập nhật
            }
        }
        private int _adminCount;
        public int AdminCount
        {
            get => _adminCount;
            set { _adminCount = value; OnPropertyChanged(); } // Bắt buộc phải có để UI tự update
        }

        private double _adminPercentage;
        public double AdminPercentage
        {
            get => _adminPercentage;
            set { _adminPercentage = value; OnPropertyChanged(); }
        }
    }
}