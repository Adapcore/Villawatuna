using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    public class OtherTypeController : UmbracoApiController
    {
        private readonly IOtherTypeService _otherTypeService;

        public OtherTypeController(IOtherTypeService otherTypeService)
        {
            _otherTypeService = otherTypeService;
        }

        [HttpGet("getItems")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _otherTypeService.GetItemsAsync();
            if (!items.Any()) return NotFound("No other items found.");
            return Ok(items);
        }
            }
}