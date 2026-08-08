using System;
using System.Collections.Generic;
using System.Text;

namespace PayFlow.Shared.Messaging
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(string topic, T eventData);
    }
}
