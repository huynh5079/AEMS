using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AEMS_WPF.Services;
using BusinessLogic.DTOs.Authentication.Login;
using BusinessLogic.DTOs.Chat;
using BusinessLogic.Service.Chat.ChatforUser;
using Microsoft.Extensions.DependencyInjection;

namespace AEMS_WPF.Views.Common
{
    public partial class ChatWindow : Window
    {
        private readonly LoggedInUserDto _currentUser;
        private readonly IChatUserService _chatUserService;
        private readonly SignalRClientService _signalRService;
        
        private ObservableCollection<ChatContactWrapper> _contacts = new();
        private ObservableCollection<ChatMessageWrapper> _messages = new();
        
        private ChatContactWrapper? _selectedContact;

        public ChatWindow(LoggedInUserDto currentUser, string? targetUserId = null)
        {
            InitializeComponent();
            _currentUser = currentUser;
            
            _chatUserService = App.ServiceProvider.GetRequiredService<IChatUserService>();
            _signalRService = App.ServiceProvider.GetRequiredService<SignalRClientService>();

            lstContacts.ItemsSource = _contacts;
            icMessages.ItemsSource = _messages;

            _signalRService.OnMessageReceived += SignalR_OnMessageReceived;

            Loaded += async (s, e) => await InitializeChatAsync(targetUserId);
        }

        private async Task InitializeChatAsync(string? targetUserId)
        {
            try
            {
                var contacts = await _chatUserService.GetContactsAsync(_currentUser.Id, _currentUser.Role);
                if (contacts == null) return;

                foreach (var c in contacts)
                {
                    _contacts.Add(new ChatContactWrapper
                    {
                        UserId = c.UserId,
                        FullName = c.FullName,
                        RoleName = c.RoleName
                    });
                }

                if (!string.IsNullOrEmpty(targetUserId))
                {
                    var target = _contacts.FirstOrDefault(c => c.UserId == targetUserId);
                    if (target != null)
                    {
                        lstContacts.SelectedItem = target;
                    }
                    else
                    {
                        // Needs to fetch the user if not in recent contacts?
                        // Simple fallback: do not select or just create dummy wrapper
                        // For MVP, user must be in contacts or logic must add them
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load contacts: {ex.Message}", "Chat Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SignalR_OnMessageReceived(ChatMessageDto message)
        {
            // If the message belongs to our current active conversation
            if (_selectedContact != null && 
               (message.SenderId == _selectedContact.UserId || message.ReceiverId == _selectedContact.UserId))
            {
                var isMine = message.SenderId == _currentUser.Id;
                _messages.Add(new ChatMessageWrapper
                {
                    Content = message.Content,
                    SentAtString = message.SentAt.ToString("HH:mm"),
                    Alignment = isMine ? "Right" : "Left",
                    BackgroundColor = isMine ? "#4F46E5" : "#F1F5F9",
                    TextColor = isMine ? "White" : "#334155"
                });
                
                // Scroll to bottom
                svMessages.ScrollToEnd();

                // Mark read if it's from them
                if (!isMine)
                {
                    _ = _chatUserService.MarkConversationReadAsync(_currentUser.Id, _selectedContact.UserId);
                }
            }
            else
            {
                // Message from someone else - update their unread count or move them to top of contacts list in future update
            }
        }

        private async void LstContacts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstContacts.SelectedItem is ChatContactWrapper contact)
            {
                _selectedContact = contact;
                txtChatHeader.Text = $"Chat with {contact.FullName} ({contact.RoleName})";
                btnSend.IsEnabled = true;
                txtMessage.IsEnabled = true;

                await LoadConversationAsync(contact.UserId);
            }
        }

        private async Task LoadConversationAsync(string otherUserId)
        {
            try
            {
                _messages.Clear();
                var history = await _chatUserService.GetConversationAsync(_currentUser.Id, _currentUser.Role, otherUserId);
                
                // Mark read
                await _chatUserService.MarkConversationReadAsync(_currentUser.Id, otherUserId);

                foreach (var msg in history.OrderBy(m => m.SentAt))
                {
                    var isMine = msg.SenderId == _currentUser.Id;
                    _messages.Add(new ChatMessageWrapper
                    {
                        Content = msg.Content,
                        SentAtString = msg.SentAt.ToString("MMM dd, HH:mm"),
                        Alignment = isMine ? "Right" : "Left",
                        BackgroundColor = isMine ? "#4F46E5" : "#F1F5F9",
                        TextColor = isMine ? "White" : "#334155"
                    });
                }

                svMessages.ScrollToEnd();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading messages: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendCurrentMessage();
        }

        private async void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SendCurrentMessage();
            }
        }

        private async Task SendCurrentMessage()
        {
            if (_selectedContact == null) return;
            var text = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            txtMessage.Text = "";
            txtMessage.Focus();

            try
            {
                // Send via Hub to broadcast to receiver seamlessly
                await _signalRService.SendPrivateMessageAsync(_selectedContact.UserId, text);

                // Add to UI immediately
                _messages.Add(new ChatMessageWrapper
                {
                    Content = text,
                    SentAtString = DateTime.Now.ToString("HH:mm"),
                    Alignment = "Right",
                    BackgroundColor = "#4F46E5",
                    TextColor = "White"
                });
                svMessages.ScrollToEnd();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not send message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _signalRService.OnMessageReceived -= SignalR_OnMessageReceived;
            base.OnClosed(e);
        }
    }

    public class ChatContactWrapper
    {
        public string UserId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string RoleName { get; set; } = "";
        public string Initial => string.IsNullOrEmpty(FullName) ? "?" : FullName[0].ToString().ToUpper();
    }

    public class ChatMessageWrapper
    {
        public string Content { get; set; } = "";
        public string SentAtString { get; set; } = "";
        public string Alignment { get; set; } = "Left";
        public string BackgroundColor { get; set; } = "#F1F5F9";
        public string TextColor { get; set; } = "#334155";
    }
}
