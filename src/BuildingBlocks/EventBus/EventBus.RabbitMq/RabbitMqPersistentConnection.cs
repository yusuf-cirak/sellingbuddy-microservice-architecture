using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EventBus.RabbitMq
{
    public sealed class RabbitMqPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly int _retryCount;
        private IConnection _connection;
        private bool _disposed;

        private readonly object _lockObject = new ();

        public RabbitMqPersistentConnection(IConnectionFactory connectionFactory, int retryCount=5)
        {
            _connectionFactory = connectionFactory;
            _retryCount = retryCount;
        }

        public bool IsConnected => _connection != null && _connection.IsOpen;

        public IModel CreateModel() => _connection.CreateModel();

        public void Dispose()
        {
            _disposed = true;
            _connection.Dispose();
        }


        public bool TryConnect()
        {
            lock (_lockObject)
            {
                var policy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (ex, time) =>
                        {

                        });

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });


                if (IsConnected)
                {
                    _connection.ConnectionShutdown += Connection_ConnectionShutdown;
                    _connection.ConnectionBlocked += Connection_ConnectionBlocked;
                    _connection.CallbackException += Connection_CallbackException;
                    return true;
                }
            }

            return false;
        }

        private void Connection_CallbackException(object? sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            if (_disposed)
            {
                // log vs.
                return;
            }

            TryConnect();
        }

        private void Connection_ConnectionBlocked(object? sender, RabbitMQ.Client.Events.ConnectionBlockedEventArgs e)
        {
            if (_disposed)
            {
                // log vs.
                return;
            }

            TryConnect();

        }

        private void Connection_ConnectionShutdown(object? sender, ShutdownEventArgs e)
        {
            if (_disposed)
            {
                // log vs.
                return;
            }

            TryConnect();

        }
    }
}
