using HotelManagement.Data;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly HotelContext _context;
        private readonly IExpenseTypeService _expenseTypeService;

        public ExpenseService(HotelContext context
            , IExpenseTypeService expenseTypeService
            )
        {
            _context = context;
            _expenseTypeService = expenseTypeService;
        }

        public async Task<IEnumerable<Expense>> GetAllAsync()
        {
            return await GetAllAsync(null, null, null, null);
        }

        public async Task<IEnumerable<Expense>> GetAllAsync(DateTime? startDate, DateTime? endDate, int? expenseTypeId, string payeeName = null)
        {
            var query = _context.Expenses.AsQueryable();

            // Apply date range filter
            if (startDate.HasValue)
            {
                query = query.Where(e => e.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.Date <= endDate.Value.Date);
            }

            // Apply expense type filter
            if (expenseTypeId.HasValue && expenseTypeId.Value > 0)
            {
                query = query.Where(e => e.ExpenseTypeID == expenseTypeId.Value);
            }

            // Apply payee name filter with wildcard search (case-insensitive)
            if (!string.IsNullOrWhiteSpace(payeeName))
            {
                query = query.Where(e => e.PayeeName != null && EF.Functions.Like(e.PayeeName, $"%{payeeName}%"));
            }

            IEnumerable<Expense> expenses = await query
                .OrderByDescending(e => e.ID)
                .ToListAsync();

            IEnumerable<ExpenseTypeDTO> lstExpenseTypes = await _expenseTypeService.GetExpenseTypesAsync();

            foreach (var item in expenses)
            {
                item.ExpenseType = lstExpenseTypes.FirstOrDefault(x => x.Id == item.ExpenseTypeID);
            }

            return expenses;
        }

        public async Task<Expense?> GetByIdAsync(int id)
        {
            return await _context.Expenses.FindAsync(id);
        }

        public async Task AddAsync(Expense expense)
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Expense expense)
        {
            _context.Expenses.Update(expense);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                await _context.SaveChangesAsync();
            }
        }
    }
}
