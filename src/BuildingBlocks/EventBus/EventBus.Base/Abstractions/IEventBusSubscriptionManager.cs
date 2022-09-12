using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base.Events;

namespace EventBus.Base.Abstractions
{
    public interface IEventBusSubscriptionManager
    {
        bool IsEmpty { get; } // Herhangi bir event'i dinliyor muyuz?

        event EventHandler<string> OnEventRemoved; // Event silindiği zaman yani dışarıdan Unsubscribe çalıştırıldığı zaman bu eventi içeride set edip tetikleyeceğiz.

        void AddSubscription<TIntegrationEvent, TIntegrationEventHandler>() where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;

        void RemoveSubscription<TIntegrationEvent, TIntegrationEventHandler>() where TIntegrationEvent : IntegrationEvent
            where TIntegrationEventHandler : IIntegrationEventHandler<TIntegrationEvent>;

        bool HasSubscriptionsForEvent<TIntegrationEvent>() where TIntegrationEvent : IntegrationEvent;
        bool HasSubscriptionsForEvent(string eventName); // Bize gönderilen event'lerin subscribe edilip(dinlenip) edilmediğini kontrol edeceğiz. 

        Type GetEventTypeByName(string eventName); // Örneğin OrderCreate'in OrderCreationHandler tipini göndereceğiz.

        void Clear(); // Bütün subscription'ları sileceğiz.

        IEnumerable<SubscriptionInfo> GetHandlersForEvent<TIntegrationEvent>()
            where TIntegrationEvent : IntegrationEvent;

        IEnumerable<SubscriptionInfo> GetHandlersForEvent(string eventName); // Bize gönderilen event'in bütün subscriptionlarını bütün handlerlarını döndüreceğiz.

        string GetEventKey<TIntegrationEvent>(); // IntegrationEvent'lerimiz için kullandığımız eventlerin bir ismi olacak ve o ismi döneceğiz.
        // Örneğin RabbitMQ'da routing key var, event'lerimizi routing keye koyacağız ve metodumuz routing key'i geri dönecek.

    }
}
