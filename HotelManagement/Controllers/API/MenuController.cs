using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    public class MenuController : UmbracoApiController
    {
        private readonly IMenuService _menuService;

        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet("getItems")]
        public async Task<IActionResult> GetItems()
        {
            var items = await _menuService.GetItemsWithCategoriesAsync();
            if (!items.Any()) return NotFound("No menu items found.");
            return Ok(items);
        }

        [HttpGet("getServiceCharge")]
        public async Task<IActionResult> GetServiceCharge()
        {
            var serviceCharge = await _menuService.GetServiceChargeAsync();
            return Ok(serviceCharge);
        }
    }
}