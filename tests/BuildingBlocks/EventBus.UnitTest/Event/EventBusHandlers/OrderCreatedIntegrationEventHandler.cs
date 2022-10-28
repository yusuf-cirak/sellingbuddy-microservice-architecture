using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Abstractions;
using EventBus.UnitTest.Event.Events;

namespace EventBus.UnitTest.Event.EventBusHandlers
{
    public class OrderCreatedIntegrationEventHandler:IIntegrationEventHandler<OrderCreatedIntegrationEvent>
    {
        public Task HandleAsync(OrderCreatedIntegrationEvent @event)
        {
            return Task.CompletedTask;
        }
    }
}
