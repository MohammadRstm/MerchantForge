using MerchForge.api.Data;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Repositories.Implementations
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly MerchForgeDbContext _db;

        public CustomerRepository(MerchForgeDbContext db)
        {
            _db = db;
        }

        public async Task<Customer?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
        }

        public async Task<Customer?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return await _db.Customers
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task AddAsync(
            Customer customer,
            CancellationToken cancellationToken = default)
        {
            await _db.Customers.AddAsync(customer, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateAsync(
            Customer customer,
            CancellationToken cancellationToken = default)
        {
            _db.Customers.Update(customer);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
