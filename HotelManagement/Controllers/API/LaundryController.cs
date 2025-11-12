using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    public class LaundryController : UmbracoApiController
    {
        private readonly ILaundryService _laundryService;

        public LaundryController(ILaundryService laundryService)
        {
            _laundryService = laundryService;
        }

        [HttpGet("getItems")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _laundryService.GetItemsAsync();
            if (!items.Any()) return NotFound("No other items found.");
            return Ok(items);
        }
    }
}