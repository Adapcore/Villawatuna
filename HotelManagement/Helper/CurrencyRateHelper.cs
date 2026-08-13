namespace HotelManagement.Helper
{
    /// <summary>
    /// Shared currency conversion rates (same source as CurrencyController).
    /// Base currency for petty cash calculations is LKR.
    /// </summary>
    public static class CurrencyRateHelper
    {
        private static readonly Dictionary<string, Dictionary<string, decimal>> Rates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["LKR"] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["USD"] = 0.00300m,
                    ["GBP"] = 0.0026m,
                    ["EUR"] = 0.0030m
                },
                ["USD"] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LKR"] = 300m,
                    ["GBP"] = 0.78m
                },
                ["GBP"] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LKR"] = 380m,
                    ["USD"] = 1.28m
                },
                ["EUR"] = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["LKR"] = 330m
                }
            };

        public static bool TryGetRate(string from, string to, out decimal rate)
        {
            rate = 0m;

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
                return false;

            from = from.Trim().ToUpperInvariant();
            to = to.Trim().ToUpperInvariant();

            if (from == to)
            {
                rate = 1m;
                return true;
            }

            if (Rates.TryGetValue(from, out var fromRates) && fromRates.TryGetValue(to, out rate))
                return true;

            if (Rates.TryGetValue(to, out var toRates) && toRates.TryGetValue(from, out var reverse))
            {
                rate = 1m / reverse;
                return true;
            }

            if (from != "LKR" && to != "LKR"
                && Rates.TryGetValue(from, out var viaFrom) && viaFrom.TryGetValue("LKR", out var toLkr)
                && Rates.TryGetValue("LKR", out var viaLkr) && viaLkr.TryGetValue(to, out var fromLkr))
            {
                rate = toLkr * fromLkr;
                return true;
            }

            return false;
        }

        public static decimal ConvertToLkr(string currency, decimal amount)
        {
            if (!TryGetRate(currency, "LKR", out var rate))
                throw new InvalidOperationException($"No conversion rate found for {currency} to LKR.");

            return Math.Round(amount * rate, 2);
        }
    }
}
