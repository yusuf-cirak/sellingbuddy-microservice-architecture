using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Abstractions;
using EventBus.Base.SubscriptionManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus:IEventBus
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventBusSubscriptionManager _eventBusSubscriptionManager;
        private EventBusConfig _eventBusConfig;

        protected BaseEventBus(IServiceProvider serviceProvider, EventBusConfig eventBusConfig)
        {
            _serviceProvider = serviceProvider;
            _eventBusSubscriptionManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
            _eventBusConfig = eventBusConfig;
        }

        public virtual string ProcessEventName(string eventName)
        {
            if (_eventBusConfig.DeleteEventPrefix)
            {
                eventName = eventName.TrimStart(_eventBusConfig.EventNamePrefix.ToArray());
            }

            if (_eventBusConfig.DeleteEventSuffix)
            {
                eventName = eventName.TrimEnd(_eventBusConfig.EventNameSuffix.ToArray());
            }

            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{_eventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose() => _eventBusConfig = null;

        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName=ProcessEventName(eventName);

            var processed = false;
            if (_eventBusSubscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = _eventBusSubscriptionManager.GetHandlersForEvent(eventName);

                using (_serviceProvider.CreateScope())
                {
                    foreach (SubscriptionInfo subscription in subscriptions)
                    {
                        var handler = _serviceProvider.GetService(subscription.HandleType);
                        if(handler==null) continue;

                        var eventType = _eventBusSubscriptionManager.GetEventTypeByName(
                            $"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");

                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

                        await (Task)concreteType.GetMethod("HandleAsync")!
                            .Invoke(handler, new object[] { integrationEvent! })!;
                    }
                }
                processed = true;
            }

            return processed;
        }

        public abstract void Publish(IntegrationEvent @event);

        public abstract void Subscribe<TIntegrationEvent, TIntegrationEventHandler>()
            where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;

        public abstract void UnSubscribe<TIntegrationEvent, TIntegrationEventHandler>()
            where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;
    }
}
