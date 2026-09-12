using System.Globalization;

namespace OnlineComputerStore.Core.Helpers
{
    public static class MoneyExtensions
    {
        public static string ToMoney(this decimal value) =>
            "$" + value.ToString("N2", CultureInfo.InvariantCulture);
    }
}
