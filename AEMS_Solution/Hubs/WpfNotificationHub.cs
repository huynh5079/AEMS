using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AEMS_Solution.Hubs
{
    public class WpfNotificationHub : Hub
    {
        private string? GetWpfUserId()
        {
            var httpContext = Context.GetHttpContext();
            var secret = httpContext?.Request.Query["wpfSecret"].ToString();
            
            if (secret == "AEMS_WPF_SECRET_2026")
            {
                return httpContext?.Request.Query["userId"].ToString();
            }
            
            return null;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetWpfUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            else
            {
                Context.Abort();
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            var userId = GetWpfUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

		public async Task SendNotification(string message)
		{
			await Clients.All.SendAsync("ReceiveNotification", message);
		}

        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Event_{eventId}");
        }

        public async Task LeaveEventGroup(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Event_{eventId}");
        }
	}
}
