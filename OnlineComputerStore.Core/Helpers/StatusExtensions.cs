namespace OnlineComputerStore.Core.Helpers
{
    public static class StatusExtensions
    {
        public static string ToStatusEmoji(this string status) => status switch
        {
            "Processing" => "⏳",
            "Shipped" => "🚚",
            "Delivered" => "✅",
            "Cancelled" => "❌",
            _ => "📦",
        };
    }
}
