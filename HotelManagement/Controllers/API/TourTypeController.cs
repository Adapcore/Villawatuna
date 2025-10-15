using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    public class TourTypeController : UmbracoApiController
    {
        private readonly ITourTypeService _tourService;

        public TourTypeController(ITourTypeService tourService)
        {
            _tourService = tourService;
        }

        [HttpGet("getItems")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _tourService.GetItemsAsync();
            if (!items.Any()) return NotFound("No other items found.");
            return Ok(items);
        }
            }
}