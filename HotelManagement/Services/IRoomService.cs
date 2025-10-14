namespace HotelManagement.Services
{
    public interface IRoomService
    {
        Task<IEnumerable<RoomTypeDto>> GetRoomCategoriesAsync();
    }

    public class RoomTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
