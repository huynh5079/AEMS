using AEMS_Solution; // Thay bằng Namespace thực tế của bạn
using DataAccess.Entities;
using System.Windows;

namespace AEMS_WPF.Views.Admin
{
    public partial class ErrorLogDetailWindow : Window
    {
        private readonly SystemErrorLog _log;

        public ErrorLogDetailWindow(SystemErrorLog log)
        {
            InitializeComponent();
            _log = log;
            LoadLogDetail();
        }

        private void LoadLogDetail()
        {
            if (_log == null) return;

            txtExceptionType.Text = _log.ExceptionType ?? "N/A";
            txtSource.Text = _log.Source ?? "N/A";
            txtMessage.Text = _log.ExceptionMessage ?? "N/A";
            txtStackTrace.Text = _log.StackTrace ?? "No stack trace available.";
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnCopyAll_Click(object sender, RoutedEventArgs e)
        {
            // Logic copy toàn bộ nội dung lỗi vào Clipboard
            string fullLog = $"Exception: {_log.ExceptionType}\n" +
                             $"Source: {_log.Source}\n" +
                             $"Message: {_log.ExceptionMessage}\n\n" +
                             $"Stack Trace:\n{_log.StackTrace}";

            Clipboard.SetText(fullLog);
            MessageBox.Show("Đã copy toàn bộ nội dung lỗi vào Clipboard!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}