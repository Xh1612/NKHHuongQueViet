using HuongQueViet.Models;

namespace HuongQueViet.Helpers
{
    public static class OrderStatusMachine
    {
        private static readonly Dictionary<OrderStatus, List<OrderStatus>> Transitions = new()
        {
            [OrderStatus.Pending] = new() { OrderStatus.Confirmed, OrderStatus.Cancelled },
            [OrderStatus.Confirmed] = new() { OrderStatus.Preparing, OrderStatus.Cancelled },
            [OrderStatus.Preparing] = new() { OrderStatus.Delivering },
            [OrderStatus.Delivering] = new() { OrderStatus.Completed },
            [OrderStatus.Completed] = new(),
            [OrderStatus.Cancelled] = new()
        };
        public static bool CanTransition(OrderStatus from, OrderStatus to) => Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }
}