using EventBus.Base;
using EventBus.Base.Abstractions;
using EventBus.Factory;
using EventBus.UnitTest.Event.EventBusHandlers;
using EventBus.UnitTest.Event.Events;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace EventBus.UnitTest
{
    public sealed class EventBusTests
    {
        private ServiceCollection services;

        public EventBusTests(ServiceCollection services)
        {
            this.services = services;
            // services.AddLogging(e=>e.AddConsole());
        }

        //[SetUp]
        //public void Setup()
        //{
        //}

        [Test]
        public void Subscribe_Event_On_RabbitMq_Test()
        {
            services.AddSingleton<IEventBus>(sp => // sp == IServiceProvider
            {
                EventBusConfig config = new()
                {
                    ConnectionRetryCount = 5,
                    SubscriberClientAppName = "EventBus.UnitTest",
                    DefaultTopicName = "SellingBuddyTopicName",
                    EventBusType = EventBusType.RabbitMQ,
                    EventNameSuffix = "IntegrationEvent", // OrderCreatedIntegrationEvent for example, that will be handled by ProcessEventName
                    Connection = new ConnectionFactory
                    {
                        HostName = "localhost",
                        Port = 15672,
                        UserName = "guest",
                        Password = "guest"
                    }
                };


                return EventBusFactory.Create(config, sp)!;
            });

            var serviceProvider = services.BuildServiceProvider();


            var eventBus = serviceProvider.GetRequiredService<IEventBus>();

            eventBus.Subscribe<OrderCreatedIntegrationEvent,OrderCreatedIntegrationEventHandler>();
            eventBus.UnSubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

        }
    }
}