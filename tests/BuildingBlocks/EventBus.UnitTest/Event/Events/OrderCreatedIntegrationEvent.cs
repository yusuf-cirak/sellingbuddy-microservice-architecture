using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Events;

namespace EventBus.UnitTest.Event.Events
{
    public sealed class OrderCreatedIntegrationEvent:IntegrationEvent
    {
        public int Id { get; set; }

        public OrderCreatedIntegrationEvent(int id)
        {
            Id = id;
        }
    }
}
