using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Core.Services;

namespace HotelManagement.Controllers
{
    [Route("api/[controller]")]
    public class MenuController : UmbracoApiController
    {
        private readonly IUmbracoContextAccessor _contextAccessor;

        public MenuController(IUmbracoContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        [HttpGet("getItems")]
        public IActionResult GetItems()
        {
            var umbracoContext = _contextAccessor.GetRequiredUmbracoContext();
            var root = umbracoContext.Content?.GetAtRoot().FirstOrDefault();

            if (root == null) return NotFound();

            var items = root
                .DescendantsOfType("menuItem")
                .Select(x => new
                {
                    Id = x.Id,
                    Name = x.Name,
                    Price = x.Value<decimal>("price")
                });

            return Ok(items);
        }

        [HttpGet("getServiceCharge")]
        public IActionResult GetServiceCharge()
        {
            var umbracoContext = _contextAccessor.GetRequiredUmbracoContext();
            var root = umbracoContext.Content?.GetAtRoot().FirstOrDefault();

            if (root == null) return NotFound();

            var menu = root
                .DescendantsOfType("menu")
                .FirstOrDefault();

            decimal serviceCharge = menu.Value<decimal>("serviceCharge");
            return Ok(serviceCharge);
        }
    }
}