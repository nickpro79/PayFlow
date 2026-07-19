using System;
using System.Collections.Generic;
using System.Text;

namespace PayFlow.Shared
{
    public interface IPaymentRepository
    {
        Task<Payment> GetByIdAsync(Guid id);
        Task AddAsync(Payment payment);
        Task UpdateAsync(Payment payment);
        Task SaveChangesAsync();
    }
}
