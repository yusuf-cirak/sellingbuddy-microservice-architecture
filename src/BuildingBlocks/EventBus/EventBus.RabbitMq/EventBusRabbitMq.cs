using System.Net.Sockets;
using System.Text;
using EventBus.Base;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMq;

public class EventBusRabbitMq : BaseEventBus
{
    private readonly RabbitMqPersistentConnection _connection;
    private readonly IConnectionFactory _connectionFactory;
    private readonly IModel _consumerChannel;


    public EventBusRabbitMq(IServiceProvider serviceProvider, EventBusConfig eventBusConfig) : base(serviceProvider, eventBusConfig)
    {
        if (EventBusConfig.Connection != null)
        {
            var connJson = JsonConvert.SerializeObject(EventBusConfig, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            _connectionFactory = JsonConvert.DeserializeObject<ConnectionFactory>(connJson)!;
        }
        else
        {
            _connectionFactory = new ConnectionFactory();
        }

        _connection = new RabbitMqPersistentConnection(_connectionFactory, EventBusConfig.ConnectionRetryCount);

        _consumerChannel = CreateConsumerModel();


        EventBusSubscriptionManager.OnEventRemoved += EventBusSubscriptionManager_OnEventRemoved;

    }


    #region Publish

    public override void Publish(IntegrationEvent @event)
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        var policy = Policy.Handle<BrokerUnreachableException>().Or<SocketException>()
            .WaitAndRetry(EventBusConfig.ConnectionRetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    // log
                });

        var eventName = @event.GetType().Name;
        eventName = ProcessEventName(eventName);

        _consumerChannel.ExchangeDeclare(exchange:EventBusConfig.DefaultTopicName,type:"direct"); // Ensure exchange exists before publishing

        var message = JsonConvert.SerializeObject(@event);

        var body = Encoding.UTF8.GetBytes(message);

        policy.Execute(() =>
        {
            var properties = _consumerChannel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent

            _consumerChannel.QueueDeclare(queue: eventName, durable: true, exclusive: false, autoDelete: false,
                arguments: null); // Ensure queue exists before publishing

            _consumerChannel.BasicPublish(exchange:EventBusConfig.DefaultTopicName,routingKey:eventName,mandatory:true,basicProperties:properties,body:body);

        });
    }

    #endregion



    #region Subscribe
    public override void Subscribe<TIntegrationEvent, TIntegrationEventHandler>()
    {
        var eventName = typeof(TIntegrationEvent).Name;
        eventName = ProcessEventName(eventName);

        if (!EventBusSubscriptionManager.HasSubscriptionsForEvent(eventName))
        {
            if (!_connection.IsConnected)
            {
                _connection.TryConnect();
            }

            var queueName = GetSubName(eventName);

            _consumerChannel.QueueDeclare(queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _consumerChannel.QueueBind
                (queue: queueName, exchange: EventBusConfig.DefaultTopicName, routingKey: eventName);

            EventBusSubscriptionManager.AddSubscription<TIntegrationEvent, TIntegrationEventHandler>();

            StartBasicConsume(eventName);
        }
    }

    private IModel CreateConsumerModel()
    {
        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        var channel = _connection.CreateModel();

        channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

        return channel;
    }


    private void StartBasicConsume(string eventName)
    {
        if (_consumerChannel != null)
        {
            var consumer = new EventingBasicConsumer(_consumerChannel);

            consumer.Received += Consumer_Received;

            _consumerChannel.BasicConsume
                (queue: GetSubName(eventName), autoAck: false, consumer: consumer);
        }
    }

    private async void Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var eventName = e.RoutingKey;
        eventName = ProcessEventName(eventName);

        var message = Encoding.UTF8.GetString(e.Body.Span);

        try
        {
            await ProcessEvent(eventName, message);
        }
        catch (Exception exception)
        {
            // logging
            Console.WriteLine(exception);
            throw;
        }

        _consumerChannel.BasicAck(e.DeliveryTag, multiple: false);
    }

    #endregion

    #region UnSubscribe

    public override void UnSubscribe<TIntegrationEvent, TIntegrationEventHandler>()
    {
        EventBusSubscriptionManager.
            RemoveSubscription<TIntegrationEvent, TIntegrationEventHandler>();
    }

    private void EventBusSubscriptionManager_OnEventRemoved(object? sender, string eventName)
    {
        eventName = ProcessEventName(eventName);

        if (!_connection.IsConnected)
        {
            _connection.TryConnect();
        }

        _consumerChannel.QueueUnbind(queue:eventName,exchange:EventBusConfig.DefaultTopicName,routingKey:eventName);

        if (EventBusSubscriptionManager.IsEmpty)
        {
            _consumerChannel.Close();
        }
    }

    #endregion




}