namespace HotelManagement.Services.Interface
{
    public interface ILaundryService
    {
        Task<IEnumerable<ItemDto>> GetItemsAsync();
    }
}
