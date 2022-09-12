using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Events;

namespace EventBus.Base.Abstractions
{
    public interface IIntegrationEventHandler<in TIntegrationEvent>
        : IIntegrationEventHandlerBase where TIntegrationEvent:IntegrationEvent
    {
        Task HandleAsync(TIntegrationEvent @event);
    }

    
}
