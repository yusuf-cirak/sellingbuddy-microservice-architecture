using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.AzureServiceBus;
using EventBus.Base;
using EventBus.Base.Abstractions;
using EventBus.Base.Events;
using EventBus.RabbitMq;

namespace EventBus.Factory
{
    public static class EventBusFactory
    {
        public static IEventBus? Create(EventBusConfig config,IServiceProvider serviceProvider)
        {

            return config.EventBusType switch
            {
                EventBusType.RabbitMQ => new EventBusRabbitMq(serviceProvider, config),
                _ => new EventBusServiceBus(serviceProvider, config)
            };
        }
    }
}
