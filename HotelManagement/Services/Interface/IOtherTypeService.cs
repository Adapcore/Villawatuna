namespace HotelManagement.Services.Interface
{
    public interface IOtherTypeService
    {
        Task<IEnumerable<ItemDto>> GetItemsAsync();
    }
}
