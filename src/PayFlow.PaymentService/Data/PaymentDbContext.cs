using Microsoft.EntityFrameworkCore;
using PayFlow.Shared;

namespace PayFlow.PaymentService.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }
        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2); // up to 18 digits total, 2 after the decimal point — standard for currency

            base.OnModelCreating(modelBuilder);
        }

    }
}
