using PayFlow.PaymentService.Data;
using PayFlow.Shared;

namespace PayFlow.PaymentService.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly PaymentDbContext _db;

        public PaymentRepository(PaymentDbContext db)
        {
            _db = db;
        }

        public async Task<Payment?> GetByIdAsync(Guid id)
        {
            return await _db.Payments.FindAsync(id);
        }

        public async Task AddAsync(Payment payment)
        {
            await _db.Payments.AddAsync(payment);
        }

        public Task UpdateAsync(Payment payment)
        {
            _db.Payments.Update(payment);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
