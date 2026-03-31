using DataAccess.Entities;
using DataAccess.Repositories.Abstraction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;

namespace AEMS_WPF.Views.Admin
{
    public class FeedbackDisplayItem
    {
        public string EventName { get; set; }
        public string ParticipantName { get; set; }
        public string ParticipantRole { get; set; }
        public string Timing { get; set; }
        public string StarRating { get; set; }
        public string Comment { get; set; }
        public string Date { get; set; }
        public double OriginalRating { get; set; }
    }

    public class AdminFeedbackViewModel : INotifyPropertyChanged
    {
        private int _totalCount;
        private string _averageRating;
        private int _positiveCount;
        private int _negativeCount;

        public int TotalCount { get => _totalCount; set { _totalCount = value; OnPropertyChanged(); } }
        public string AverageRating { get => _averageRating; set { _averageRating = value; OnPropertyChanged(); } }
        public int PositiveCount { get => _positiveCount; set { _positiveCount = value; OnPropertyChanged(); } }
        public int NegativeCount { get => _negativeCount; set { _negativeCount = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class AdminFeedbackPage : Page
    {
        private readonly IUnitOfWork _uow;
        private List<Feedback> _allFeedbacksFromDb = new List<Feedback>();
        private AdminFeedbackViewModel _viewModel;

        public AdminFeedbackPage(IUnitOfWork uow)
        {
            InitializeComponent();
            _uow = uow;
            _viewModel = new AdminFeedbackViewModel();
            this.DataContext = _viewModel;
            this.Loaded += async (s, e) => await LoadRealDataAsync();
        }

        private async Task LoadRealDataAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Nạp Item cho Rating Filter
                if (cbRatingFilter.Items.Count == 0)
                {
                    cbRatingFilter.Items.Add("All Ratings");
                    cbRatingFilter.Items.Add("5 Stars");
                    cbRatingFilter.Items.Add("4 Stars");
                    cbRatingFilter.Items.Add("3 Stars & Below");
                    cbRatingFilter.SelectedIndex = 0;
                }

                // Lấy dữ liệu Feedback
                var feedbacks = await _uow.Feedbacks.GetAllAsync(
                    null,
                    query => query.Include(f => f.Event)
                                  .Include(f => f.Student)
                                  .ThenInclude(s => s.User)
                );

                _allFeedbacksFromDb = feedbacks.ToList();

                // SỬA LỖI: Ép kiểu Enum sang int trước khi so sánh
                _viewModel.TotalCount = _allFeedbacksFromDb.Count;

                _viewModel.PositiveCount = _allFeedbacksFromDb.Count(f =>
                    f.RatingEvent.HasValue && (int)f.RatingEvent.Value >= 4);

                _viewModel.NegativeCount = _allFeedbacksFromDb.Count(f =>
                    f.RatingEvent.HasValue && (int)f.RatingEvent.Value <= 3);

                var rated = _allFeedbacksFromDb.Where(f => f.RatingEvent.HasValue).ToList();

                // SỬA LỖI: Ép kiểu sang int để tính Average
                _viewModel.AverageRating = rated.Any()
                    ? rated.Average(f => (int)f.RatingEvent.Value).ToString("F1")
                    : "0.0";

                DisplayData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu feedback: " + ex.Message);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private string GetStarRatingString(double? rating)
        {
            int r = (int)Math.Round(rating ?? 0);
            if (r < 0) r = 0;
            if (r > 5) r = 5;
            return new string('★', r) + new string('☆', 5 - r);
        }

        // Đã bỏ phân trang, hiển thị TOÀN BỘ dữ liệu lên DataGrid
        private void DisplayData()
        {
            string selectedRating = cbRatingFilter.SelectedItem?.ToString() ?? "All Ratings";

            var filteredList = _allFeedbacksFromDb.Where(f =>
            {
                // Ép kiểu Enum sang số để filter
                double r = f.RatingEvent.HasValue ? (int)f.RatingEvent.Value : 0;

                if (selectedRating == "5 Stars") return r >= 5;
                if (selectedRating == "4 Stars") return r >= 4 && r < 5;
                if (selectedRating == "3 Stars & Below") return r <= 3;
                return true; // All Ratings
            })
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDisplayItem
            {
                EventName = f.Event?.Title ?? "Deleted Event",
                ParticipantName = f.Student?.User?.FullName ?? "Unknown User",
                ParticipantRole = f.Student?.User?.Role?.ToString() ?? "Student",

                // SỬA LỖI: Dùng StartTime và EndTime, bỏ .HasValue và .Value
                Timing = f.Event != null
                         ? $"{f.Event.StartTime:dd MMM} - {f.Event.EndTime:dd MMM yyyy}"
                         : "N/A",

                OriginalRating = f.RatingEvent.HasValue ? (int)f.RatingEvent.Value : 0,
                StarRating = GetStarRatingString(f.RatingEvent.HasValue ? (double)(int)f.RatingEvent.Value : 0),
                Comment = string.IsNullOrWhiteSpace(f.Comment) ? "No comment provided." : f.Comment,
                Date = f.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();

            dgvFeedbacks.ItemsSource = filteredList;
        }

        // Tự động lọc dữ liệu khi xổ danh sách chọn số Sao
        private void CbRatingFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allFeedbacksFromDb != null && _allFeedbacksFromDb.Any())
            {
                DisplayData();
            }
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}