using HotelManagement.Data;
using HotelManagement.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelManagement.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly HotelContext _dbContext;

        public CustomerService(HotelContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _dbContext.Customers
                .OrderBy(c => c.FirstName)
                .ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _dbContext.Customers.FindAsync(id);
        }

        public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            IQueryable<Customer> query = _dbContext.Customers.Where(c => c.Email == email);
            if (excludeId.HasValue)
            {
                query = query.Where(c => c.ID != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Customer> CreateAsync(Customer customer)
        {
            _dbContext.Customers.Add(customer);
            await _dbContext.SaveChangesAsync();
            return customer;
        }

        public async Task UpdateAsync(Customer customer)
        {
            _dbContext.Entry(customer).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
        }
    }
}
