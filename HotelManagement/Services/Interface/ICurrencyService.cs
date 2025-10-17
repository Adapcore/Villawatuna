using HotelManagement.Models.DTO;

namespace HotelManagement.Services.Interface
{
    public interface ICurrencyService
    {
        Task<IEnumerable<CurrencyDTO>> GetCurencyTypesAsync();
    }
}
