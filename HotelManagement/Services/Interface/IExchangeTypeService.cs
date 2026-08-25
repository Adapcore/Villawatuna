namespace HotelManagement.Services.Interface
{
    public interface IExchangeTypeService
    {
        Task<IEnumerable<ItemDto>> GetItemsAsync();
        Task<ItemDto?> GetCurrencyExchangeItemAsync();
    }
}
