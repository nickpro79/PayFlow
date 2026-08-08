using System;
using System.Collections.Generic;
using System.Text;

namespace PayFlow.Shared.Events
{
    public class PaymentSucceededEvent
    {
        public Guid PaymentId {  get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    }
}
