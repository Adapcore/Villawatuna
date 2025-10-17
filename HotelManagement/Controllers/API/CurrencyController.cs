using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    public class CurrencyController : UmbracoApiController
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet("getCurencyTypes")]
        public async Task<IActionResult> GetCurencyTypes()
        {
            var items = await _currencyService.GetCurencyTypesAsync();
            if (!items.Any()) return NotFound("No currency types found.");
            return Ok(items);
        }
    }
}