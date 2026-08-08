using System;
using System.Collections.Generic;
using System.Text;

namespace PayFlow.Shared.Processing
{
    public interface IPaymentProcessor
    {
        Task<bool> ProcessAsync(decimal amount, string currency);
    }
}
