using System;
using System.Collections.Generic;
using System.Text;

namespace PayFlow.Shared
{
    public class Payment
    {
        public Guid Id { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "PENDING";// PENDING, SUCCESS, FAILED
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
