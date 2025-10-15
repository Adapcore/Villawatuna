using HotelManagement.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace HotelManagement.Controllers.API
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomController : ControllerBase
    {
        private readonly IRoomService _roomService;

        public RoomController(IRoomService roomService)
        {
            _roomService = roomService;
        }

        [HttpGet("GetRoomCategories")]
        public async Task<IActionResult> GetRoomCategories()
        {
            var items = await _roomService.GetRoomCategoriesAsync();
            if (!items.Any()) return NotFound("No room categories found.");
            return Ok(items);
        }

    }
}
