using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base;
using EventBus.Base.Abstractions;
using EventBus.Base.Events;

namespace EventBus.Factory
{
    public static class EventBusFactory
    {
        public static IEventBus? Create<TEventBusType>(EventBusConfig config,IServiceProvider serviceProvider) where TEventBusType: BaseEventBus,new()
        {
            return Activator.CreateInstance(typeof(TEventBusType), config, serviceProvider) as TEventBusType;
        }
    }
}
