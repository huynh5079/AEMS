using System.Security.Claims;
using BusinessLogic.DTOs;
using BusinessLogic.Service.Chat.ChatforUser;
using BusinessLogic.Service.System;
using DataAccess.Enum;
using Microsoft.AspNetCore.SignalR;
using BusinessLogic.DTOs.Chat;

namespace AEMS_Solution.Hubs
{
    public class WpfChatHub : Hub
    {
        private readonly IChatUserService _chatUserService;
        private readonly IChatPresenceTracker _presenceTracker;
        private readonly INotificationService _notificationService;

        public WpfChatHub(IChatUserService chatUserService, IChatPresenceTracker presenceTracker, INotificationService notificationService)
        {
            _chatUserService = chatUserService;
            _presenceTracker = presenceTracker;
            _notificationService = notificationService;
        }

        private (string? userId, string? role) GetWpfUser()
        {
            var httpContext = Context.GetHttpContext();
            var secret = httpContext?.Request.Query["wpfSecret"].ToString();
            
            if (secret == "AEMS_WPF_SECRET_2026")
            {
                return (httpContext?.Request.Query["userId"].ToString(), httpContext?.Request.Query["userRole"].ToString());
            }
            
            return (null, null);
        }

        public override async Task OnConnectedAsync()
        {
            var (userId, _) = GetWpfUser();
            if (!string.IsNullOrEmpty(userId))
            {
                _presenceTracker.UserConnected(userId);
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            else
            {
                Context.Abort();
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var (userId, _) = GetWpfUser();
            if (!string.IsNullOrEmpty(userId))
            {
                _presenceTracker.UserDisconnected(userId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string receiverUserId, string content)
        {
            var (senderUserId, senderRole) = GetWpfUser();
            if (string.IsNullOrWhiteSpace(senderUserId))
            {
                throw new HubException("Không xác định được người gửi.");
            }

            try
            {
                var message = await _chatUserService.SendPrivateMessageAsync(senderUserId, senderRole ?? "", receiverUserId, content);

                await Clients.Group(senderUserId).SendAsync("ReceivePrivateMessage", message);
                await Clients.Group(receiverUserId).SendAsync("ReceivePrivateMessage", message);

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    ReceiverId = receiverUserId,
                    Title = "Tin nhắn mới",
                    Message = content.Length > 60 ? content[..60] + "..." : content,
                    Type = NotificationType.NewChatMessage,
                    RelatedEntityId = senderUserId
                });
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task RecallPrivateMessage(string messageId)
        {
            var (userId, _) = GetWpfUser();
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("Không xác định được người dùng.");
            }

            try
            {
                var message = await _chatUserService.RecallMessageAsync(userId, messageId);
                await Clients.Group(message.SenderId).SendAsync("MessageRecalled", message);
                await Clients.Group(message.ReceiverId).SendAsync("MessageRecalled", message);
            }
            catch (Exception ex)
            {
                throw new HubException(ex.Message);
            }
        }

        public async Task MarkConversationRead(string otherUserId)
        {
            var (userId, _) = GetWpfUser();
            if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(otherUserId))
            {
                await _chatUserService.MarkConversationReadAsync(userId, otherUserId);
                await Clients.Group(otherUserId).SendAsync("ConversationRead", new
                {
                    ReaderUserId = userId,
                    OtherUserId = otherUserId
                });
            }
        }
    }
}
