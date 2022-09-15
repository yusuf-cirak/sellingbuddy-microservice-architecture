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
        public readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscriptionManager EventBusSubscriptionManager;
        public EventBusConfig EventBusConfig { get; set; }

        protected BaseEventBus(IServiceProvider serviceProvider, EventBusConfig eventBusConfig)
        {
            ServiceProvider = serviceProvider;
            EventBusSubscriptionManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
            EventBusConfig = eventBusConfig;
        }

        public virtual string ProcessEventName(string eventName)
        {
            if (EventBusConfig.DeleteEventPrefix)
            {
                eventName = eventName.TrimStart(EventBusConfig.EventNamePrefix.ToArray());
            }

            if (EventBusConfig.DeleteEventSuffix)
            {
                eventName = eventName.TrimEnd(EventBusConfig.EventNameSuffix.ToArray());
            }

            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{EventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            EventBusConfig = null;
            EventBusSubscriptionManager.Clear();

        }

        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName=ProcessEventName(eventName);

            var processed = false;
            if (EventBusSubscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = EventBusSubscriptionManager.GetHandlersForEvent(eventName);

                using (ServiceProvider.CreateScope())
                {
                    foreach (SubscriptionInfo subscription in subscriptions)
                    {
                        var handler = ServiceProvider.GetService(subscription.HandleType);
                        if(handler==null) continue;

                        var eventType = EventBusSubscriptionManager.GetEventTypeByName(
                            $"{EventBusConfig.EventNamePrefix}{eventName}{EventBusConfig.EventNameSuffix}");

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
