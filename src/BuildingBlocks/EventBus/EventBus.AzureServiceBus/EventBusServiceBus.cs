using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Newtonsoft.Json;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient? _topicClient;
        private ManagementClient _managementClient;
        private ILogger? _logger;
        public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfig eventBusConfig) : base(serviceProvider, eventBusConfig)
        {
            _logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
            _managementClient = new ManagementClient(eventBusConfig.EventBusConnectionString);
            _topicClient = CreateTopicClient();
        }

        private ITopicClient CreateTopicClient()
        {
            if (_topicClient == null || _topicClient.IsClosedOrClosing)
            {
                _topicClient =
                    new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, RetryPolicy.Default);
            }

            if (!_managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
            {
                _managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
            }

            return _topicClient;
        }

        public override void Publish(IntegrationEvent @event)
        {
            var eventName = @event.GetType().Name; // example : OrderCreatedIntegrationEvent
            eventName = ProcessEventName(eventName); // example : OrderCreated

            var eventStr = JsonConvert.SerializeObject(@event);
            var bodyArr = Encoding.UTF8.GetBytes(eventStr);

            var message = new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArr,
                Label = eventName
            };
            _topicClient?.SendAsync(message).GetAwaiter().GetResult();
        }

        #region Subscribe

        public override void Subscribe<TIntegrationEvent, TIntegrationEventHandler>()
        {
            var eventName = typeof(TIntegrationEvent)?.GetType().Name; // example : OrderCreatedIntegrationEvent
            eventName = ProcessEventName(eventName!); // example : OrderCreated

            if (!EventBusSubscriptionManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptionClient = CreateSubscriptionClientIfNotExists(eventName);

                RegisterSubscriptionClientMessageHandler(subscriptionClient);
            }
            _logger?.LogInformation($"Subscribing to event '{eventName}' with '{nameof(TIntegrationEventHandler)}'");

            EventBusSubscriptionManager.AddSubscription<TIntegrationEvent, TIntegrationEventHandler>();

        }

        private ISubscriptionClient CreateSubscriptionClientIfNotExists(string eventName)
        {
            var subClient = CreateSubscriptionClient(eventName);
            var exists = _managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();

            if (!exists)
            {
                _managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName))
                    .GetAwaiter().GetResult();

                RemoveDefaultRule(subClient);
            }

            CreateRuleIfNotExists(eventName, subClient);

            return subClient;
        }

        private SubscriptionClient CreateSubscriptionClient(string eventName)
        {
            return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName,
                GetSubName(eventName));
        }

        private void RemoveDefaultRule(SubscriptionClient client)
        {
            try
            {
                client.RemoveRuleAsync(RuleDescription.DefaultRuleName)
                    .GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger?.LogWarning($"The messaging entity {RuleDescription.DefaultRuleName} not found");
            }
        }

        private void CreateRuleIfNotExists(string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExists;

            try
            {
                var rule = _managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult();
                ruleExists = rule != null;
            }
            catch
            {
                // Azure management client does not have RuleExists method
                ruleExists = false;
            }

            if (!ruleExists)
            {
                subscriptionClient.AddRuleAsync(new RuleDescription
                {
                    Name = eventName,
                    Filter = new CorrelationFilter { Label = eventName }
                }).GetAwaiter().GetResult();
            }
        }

        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(async (message, token) =>
            {
                var eventName = $"{message.Label}";
                var messageData = Encoding.UTF8.GetString(message.Body);

                if (await ProcessEvent(eventName, messageData))
                {
                    await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                }
            }, new MessageHandlerOptions(ExceptionReceivedHandler) { MaxConcurrentCalls = 10, AutoComplete = false });
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            _logger?.LogError(ex, $"Error handling message : {ex.Message}, Context : {context}");

            return Task.CompletedTask;
        }

        #endregion


        public override void UnSubscribe<TIntegrationEvent, TIntegrationEventHandler>()
        {
            var eventName = typeof(TIntegrationEvent).Name;

            try
            {
                var subscriptionClient = CreateSubscriptionClient(eventName);

                subscriptionClient.RemoveRuleAsync(eventName).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {
                _logger?.LogWarning($"The messaging entity '{eventName} could not be found'");
            }

            _logger?.LogInformation($"Unsubscribing from event '{eventName}'");

            EventBusSubscriptionManager.RemoveSubscription<TIntegrationEvent, TIntegrationEventHandler>();
        }


        public override void Dispose()
        {
            base.Dispose();

            _topicClient.CloseAsync().GetAwaiter().GetResult();
            _managementClient.CloseAsync().GetAwaiter().GetResult();

            _topicClient = null;
            _managementClient = null;
        }




    }
}
