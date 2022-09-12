using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Abstractions;
using EventBus.Base.Events;

namespace EventBus.Base.SubscriptionManagers
{
    public class InMemoryEventBusSubscriptionManager:IEventBusSubscriptionManager
    {

        private readonly Dictionary<string, List<SubscriptionInfo>> _handlers;
        private readonly List<Type> _eventTypes;
        public event EventHandler<string>? OnEventRemoved;
        public Func<string, string> _eventNameGetter; // OrderCreateIntegrationEvent adında bir class'ımız olduğunda, bunun bazı sonunu kırpmak isteyebiliriz. RabbitMQ'ya kuyruğa bu şekilde eklemektense kesip ekleme işi yapacağız. Bunun nasıl yapacağını bir Func olarak dışarıdan alacağız.

        public InMemoryEventBusSubscriptionManager(Func<string,string> eventNameGetter)
        {
            _handlers = new();
            _eventTypes = new ();
            _eventNameGetter = eventNameGetter;
        }
        public bool IsEmpty => !_handlers.Keys.Any();
        public void Clear() => _handlers.Clear();
        public bool HasSubscriptionsForEvent(string eventName) => _handlers.ContainsKey(eventName);
        public IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName) => _handlers[eventName];

        public Type? GetEventTypeByName(string eventName) => _eventTypes.SingleOrDefault(e => e.Name==eventName);


        public void AddSubscription<TIntegrationEvent, TIntegrationEventHandler>() where TIntegrationEvent : IntegrationEvent where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            var eventName = GetEventKey<TIntegrationEvent>();

            AddSubscription(typeof(TIntegrationEventHandler), eventName);

            if (!_eventTypes.Contains(typeof(TIntegrationEvent)))
            {
                _eventTypes.Add(typeof(TIntegrationEvent));
            }
        }

        private void AddSubscription(Type handlerType, string eventName)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                _handlers.Add(eventName,new List<SubscriptionInfo>());
            }

            if (_handlers[eventName].Any(s=>s.HandleType==handlerType))
            {
                throw new ArgumentException($"Handler type {handlerType.Name} is already registered for '{eventName}'",nameof(handlerType));
            }

            _handlers[eventName].Add(SubscriptionInfo.Typed(handlerType));
        }

        public void RemoveSubscription<TIntegrationEvent, TIntegrationEventHandler>() where TIntegrationEvent : IntegrationEvent where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>
        {
            var handlerToRemove = FindSubscriptionToRemove<TIntegrationEvent,TIntegrationEventHandler>();

            var eventName = GetEventKey<TIntegrationEvent>();
            
            RemoveSubscriptionHandler(eventName,handlerToRemove);
        }
        private void RemoveSubscriptionHandler(string eventName, SubscriptionInfo? subsToRemove)
        {
            if (subsToRemove != null)
            {
                _handlers[eventName].Remove(subsToRemove);
                if (_handlers[eventName].Any())
                {
                    _handlers.Remove(eventName);

                    var eventType = _eventTypes.SingleOrDefault(e => e.Name == eventName);
                    if (eventType != null)
                    {
                        _eventTypes.Remove(eventType);
                    }
                    RaiseOnEventRemoved(eventName);
                }
            }
        }
        private SubscriptionInfo FindSubscriptionToRemove<TIntegrationEvent, TIntegrationEventHandler>()
        {
            var eventName = GetEventKey<TIntegrationEvent>();
            return FindSubscriptionToRemove(eventName, typeof(TIntegrationEventHandler))!;
        }

        private SubscriptionInfo? FindSubscriptionToRemove(string eventName, Type handlerType)
        {
            if (!HasSubscriptionsForEvent(eventName))
            {
                return null;
            }

            return _handlers[eventName].SingleOrDefault(h => h.HandleType == handlerType);
        }

        private void RaiseOnEventRemoved(string eventName)
        {
            var handler = OnEventRemoved;
            handler?.Invoke(this,eventName);
        }

        public bool HasSubscriptionsForEvent<TIntegrationEvent>() where TIntegrationEvent : IntegrationEvent
        {
            var key = GetEventKey<TIntegrationEvent>();
            return HasSubscriptionsForEvent(key);
        }


        public IEnumerable<SubscriptionInfo> GetHandlersForEvent<TIntegrationEvent>() where TIntegrationEvent : IntegrationEvent
        {
            var key = GetEventKey<TIntegrationEvent>();
            return GetHandlersForEvent(key);
        }

       

        public string GetEventKey<TIntegrationEvent>()
        {
            var eventName = typeof(TIntegrationEvent).Name;
            return _eventNameGetter(eventName);
        }
    }
}
