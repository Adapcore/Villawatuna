namespace HotelManagement.Services.Interface
{
    public interface ITourTypeService
    {
        Task<IEnumerable<ItemDto>> GetItemsAsync();
        Task<string?> GetDefaultCurrencyForBillingAsync();
    }
}
