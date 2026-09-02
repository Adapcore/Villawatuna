using HotelManagement.Data;
using HotelManagement.Models.DTO;
using HotelManagement.Models.Entities;
using HotelManagement.Services.Interface;
using HotelManagement.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

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

        public async Task<IEnumerable<Expense>> GetAllAsync(DateTime? startDate, DateTime? endDate, int? expenseTypeId, string? payeeName = null)
        {
            var query = BuildFilteredQuery(startDate, endDate, expenseTypeId, payeeName)
                .OrderByDescending(e => e.ID);

            var expenses = await query.ToListAsync();
            await AttachExpenseTypesAsync(expenses);
            return expenses;
        }

        public async Task<(IPagedList<Expense> Items, decimal TotalAmount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int? expenseTypeId = null,
            string? payeeName = null)
        {
            var filtered = BuildFilteredQuery(startDate, endDate, expenseTypeId, payeeName);

            var totalAmount = await filtered.SumAsync(e => (decimal?)e.Amount) ?? 0m;

            var paged = filtered
                .OrderByDescending(e => e.ID)
                .ToPagedList(pageNumber, pageSize);

            await AttachExpenseTypesAsync(paged);
            return (paged, totalAmount);
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
            var existing = await _context.Expenses.FindAsync(expense.ID)
                ?? throw new InvalidOperationException("Expense not found.");

            existing.Date = expense.Date;
            existing.ExpenseTypeID = expense.ExpenseTypeID;
            existing.PayeeName = expense.PayeeName;
            existing.Amount = expense.Amount;
            existing.PaymentMethod = expense.PaymentMethod;
            existing.Description = expense.Description;

            if (!string.IsNullOrEmpty(expense.Attachment))
                existing.Attachment = expense.Attachment;

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

        private IQueryable<Expense> BuildFilteredQuery(
            DateTime? startDate,
            DateTime? endDate,
            int? expenseTypeId,
            string? payeeName)
        {
            var query = _context.Expenses
                .AsNoTracking()
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(e => e.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(e => e.Date <= endDate.Value.Date);

            if (expenseTypeId.HasValue && expenseTypeId.Value > 0)
                query = query.Where(e => e.ExpenseTypeID == expenseTypeId.Value);

            if (!string.IsNullOrWhiteSpace(payeeName))
                query = query.Where(e => e.PayeeName != null && EF.Functions.Like(e.PayeeName, $"%{payeeName}%"));

            return query;
        }

        private async Task AttachExpenseTypesAsync(IEnumerable<Expense> expenses)
        {
            var expenseTypes = (await _expenseTypeService.GetExpenseTypesAsync()).ToList();
            foreach (var item in expenses)
            {
                item.ExpenseType = expenseTypes.FirstOrDefault(x => x.Id == item.ExpenseTypeID);
            }
        }
    }
}
