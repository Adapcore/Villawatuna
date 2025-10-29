using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly Dictionary<string, Dictionary<string, decimal>> _currencyRates;

        public CurrencyController()
        {
            // Initialize currency rates (moved from JS)
            _currencyRates = new Dictionary<string, Dictionary<string, decimal>>
            {
                ["LKR"] = new Dictionary<string, decimal>
                {
                    ["USD"] = 0.00300m,
                    ["GBP"] = 0.0026m,
                    ["EUR"] = 0.0030m
                },
                ["USD"] = new Dictionary<string, decimal>
                {
                    ["LKR"] = 300m,
                    ["GBP"] = 0.78m
                },
                ["GBP"] = new Dictionary<string, decimal>
                {
                    ["LKR"] = 380m,
                    ["USD"] = 1.28m
                },
                ["EUR"] = new Dictionary<string, decimal>
                {
                    ["LKR"] = 330m
                }
            };
        }

        [HttpGet("getCurrencyRate")]
        public IActionResult GetCurrencyRate(string from, string to)
        {
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                return BadRequest(new { message = "From and To currency codes are required." });
            }

            if (from == to)
            {
                return Ok(new { rate = 1.0m });
            }

            // Try direct conversion
            if (_currencyRates.ContainsKey(from) && _currencyRates[from].ContainsKey(to))
            {
                return Ok(new { rate = _currencyRates[from][to] });
            }

            // Try reverse conversion
            if (_currencyRates.ContainsKey(to) && _currencyRates[to].ContainsKey(from))
            {
                return Ok(new { rate = 1.0m / _currencyRates[to][from] });
            }

            // Try conversion via LKR as base currency
            if (from != "LKR" && to != "LKR")
            {
                if (_currencyRates.ContainsKey(from) && _currencyRates[from].ContainsKey("LKR") &&
                    _currencyRates.ContainsKey("LKR") && _currencyRates["LKR"].ContainsKey(to))
                {
                    var rate = _currencyRates[from]["LKR"] * _currencyRates["LKR"][to];
                    return Ok(new { rate });
                }
            }

            return NotFound(new { message = $"No conversion rate found for {from} to {to}" });
        }

        [HttpGet("getAllRates")]
        public IActionResult GetAllRates()
        {
            return Ok(new { rates = _currencyRates });
        }
    }
}