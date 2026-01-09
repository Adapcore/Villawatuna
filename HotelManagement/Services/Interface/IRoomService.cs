namespace HotelManagement.Services.Interface
{
    public interface IRoomService
    {
        Task<IEnumerable<ItemDto>> GetRoomCategoriesAsync();
        Task<string?> GetDefaultCurrencyForBillingAsync();
    }   
}
