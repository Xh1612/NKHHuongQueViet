using Microsoft.AspNetCore.SignalR;

namespace HuongQueViet.Hubs
{
    public class OrderStatusHub : Hub
    {
        public async Task JoinOrderGroup(int orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }
    }
}