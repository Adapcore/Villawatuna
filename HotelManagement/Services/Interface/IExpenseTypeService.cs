using HotelManagement.Models.DTO;

namespace HotelManagement.Services.Interface
{
    public interface IExpenseTypeService
    {
        Task<IEnumerable<ExpenseTypeDTO>> GetExpenseTypesAsync();
    }   
}
