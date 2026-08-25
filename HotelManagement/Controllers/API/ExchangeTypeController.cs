using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    public class ExchangeTypeController : UmbracoApiController
    {
        private readonly IExchangeTypeService _exchangeTypeService;

        public ExchangeTypeController(IExchangeTypeService exchangeTypeService)
        {
            _exchangeTypeService = exchangeTypeService;
        }

        [HttpGet("getItems")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _exchangeTypeService.GetItemsAsync();
            if (!items.Any()) return NotFound("No exchange items found.");
            return Ok(items);
        }
    }
}
