using System;
using System.Threading.Tasks;
using System.Windows;
using BusinessLogic.DTOs.Chat;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace AEMS_WPF.Services
{
    public class SignalRClientService
    {
        private HubConnection? _notificationHub;
        private HubConnection? _chatHub;
        private readonly string _baseUrl;

        public event Action<ChatMessageDto>? OnMessageReceived;
        public event Action<ChatMessageDto>? OnMessageRecalled;
        public event Action<string, string>? OnConversationRead; // readerUserId, otherUserId

        public SignalRClientService(IConfiguration configuration)
        {
            _baseUrl = configuration["AppSettings:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:7149";
        }

        public async Task StartAsync(string userId, string userRole)
        {
            if (string.IsNullOrEmpty(userId)) return;
            
            var queryAuth = $"?wpfSecret=AEMS_WPF_SECRET_2026&userId={userId}&userRole={userRole}";

            // 1. Setup Notification Hub
            if (_notificationHub == null)
            {
                _notificationHub = new HubConnectionBuilder()
                    .WithUrl($"{_baseUrl}/hub/wpf/notification{queryAuth}")
                    .WithAutomaticReconnect()
                    .Build();

                _notificationHub.On<string>("ReceiveNotification", (message) =>
                {
                    Application.Current.Dispatcher.Invoke(() => ShowNotification(message));
                });
            }

            // 2. Setup Chat Hub
            if (_chatHub == null)
            {
                _chatHub = new HubConnectionBuilder()
                    .WithUrl($"{_baseUrl}/hub/wpf/chat{queryAuth}")
                    .WithAutomaticReconnect()
                    .Build();

                _chatHub.On<ChatMessageDto>("ReceivePrivateMessage", (message) =>
                {
                    Application.Current.Dispatcher.Invoke(() => OnMessageReceived?.Invoke(message));
                });

                _chatHub.On<ChatMessageDto>("MessageRecalled", (message) =>
                {
                    Application.Current.Dispatcher.Invoke(() => OnMessageRecalled?.Invoke(message));
                });

                // The server sends an anonymous object. Wait, it's easier to map separate params.
                // server: await Clients.Group(otherUserId).SendAsync("ConversationRead", new { ReaderUserId = userId, OtherUserId = otherUserId });
                _chatHub.On<dynamic>("ConversationRead", (data) =>
                {
                    Application.Current.Dispatcher.Invoke(() => 
                    {
                        var reader = (string)data.ReaderUserId;
                        var other = (string)data.OtherUserId;
                        OnConversationRead?.Invoke(reader, other);
                    });
                });
            }

            try
            {
                Debug.WriteLine($"[SignalR] Starting connection to hubs for user {userId}...");
                if (_notificationHub.State == HubConnectionState.Disconnected)
                    await _notificationHub.StartAsync();
                    
                if (_chatHub.State == HubConnectionState.Disconnected)
                    await _chatHub.StartAsync();

                Debug.WriteLine($"[SignalR] Connected successfully as User: {userId} ({userRole}).");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SignalR] Connection Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[SignalR] Inner Error: {ex.InnerException.Message}");
                }
            }
        }

        public async Task SendPrivateMessageAsync(string receiverUserId, string content)
        {
            if (_chatHub == null || _chatHub.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Chat hub is not connected.");

            await _chatHub.InvokeAsync("SendPrivateMessage", receiverUserId, content);
        }

        public async Task RecallPrivateMessageAsync(string messageId)
        {
            if (_chatHub == null || _chatHub.State != HubConnectionState.Connected) return;
            await _chatHub.InvokeAsync("RecallPrivateMessage", messageId);
        }

        public async Task MarkConversationReadAsync(string otherUserId)
        {
             if (_chatHub == null || _chatHub.State != HubConnectionState.Connected) return;
             await _chatHub.InvokeAsync("MarkConversationRead", otherUserId);
        }

        private void ShowNotification(string message)
        {
            MessageBox.Show(message, "System Notification", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public async Task StopAsync()
        {
            if (_notificationHub != null)
            {
                await _notificationHub.StopAsync();
                await _notificationHub.DisposeAsync();
                _notificationHub = null;
            }

            if (_chatHub != null)
            {
                await _chatHub.StopAsync();
                await _chatHub.DisposeAsync();
                _chatHub = null;
            }
        }
    }
}
