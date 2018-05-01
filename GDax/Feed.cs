using GDax.Enums;
using GDax.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDax
{
    public delegate void PriceUpdatedEventHandler(IProduct product, TickerResponse data);

    public interface IFeed
    {
        event PriceUpdatedEventHandler PriceUpdated;

        void Subscribe(IProduct product);

        void Unsubscribe(IProduct product);
    }

    public class Feed : IFeed, IDisposable
    {
        public event PriceUpdatedEventHandler PriceUpdated;

        private static readonly object _connectionLock = new object();
        private bool _connecting = false;
        private int[] _retryFactor = new int[] { 0, 1, 2, 4, 8 };
        private int _connectionAttempts = 0;
        private List<IProduct> _subscribed = new List<IProduct>();
        private List<IProduct> _subscriptions = new List<IProduct>();
        private Dictionary<IProduct, long> _lastSequence = new Dictionary<IProduct, long>();
        private CancellationTokenSource _token;
        private WebSocket _socket;

        public void Subscribe(IProduct product)
        {
            if (_subscribed.Contains(product))
            {
                EnsureSubscription();
                return;
            }

            _subscribed.Add(product);
            EnsureConnection();
        }

        public void Unsubscribe(IProduct product)
        {
            if (!_subscribed.Contains(product))
            {
                EnsureSubscription();
                return;
            }

            _subscribed.Remove(product);
            EnsureConnection();

            if (_lastSequence.ContainsKey(product))
                _lastSequence.Remove(product);
        }

        private void EnsureConnection()
        {
            var retry = false;

            // Ensure that this is only ran once concurrently
            if (_connecting) return;
            lock (_connectionLock)
            {
                if (_connecting) return;
                _connecting = true;
            }

            // If the connection is already establish then don't do anything.
            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                EnsureSubscription();

                lock (_connectionLock)
                {
                    _connecting = false;
                }
                return;
            }

            if (_socket != null)
            {
                _token?.Cancel();
                _token?.Dispose();
                _socket.Dispose();
                _socket = null;
            }

            _token = new CancellationTokenSource();
            _socket = SystemClientWebSocket.CreateClientWebSocket();

            try
            {
                Debug.WriteLine("Attempting to connect to GDAX API...");
                _socket.ConnectAsync(new Uri("wss://ws-feed.gdax.com"), CancellationToken.None).Wait();
                Debug.WriteLine("Connected");

                EnsureSubscription();

                _connectionAttempts = 0;
                Task.Run(() => ReceiveData());
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Any(e => e is WebSocketException))
                {
                    _subscriptions.Clear();
                    Debug.WriteLine($"Failed to connect to GDAX API. Error: {ex.InnerExceptions[0].Message}");
                    retry = true;
                }
            }
            finally
            {
                // Don't forget to unlock
                lock (_connectionLock)
                {
                    _connecting = false;
                }

                if (retry)
                {
                    // Round robin exponential delay with a 5 minute cap.
                    var delay = Math.Min(300, _connectionAttempts * _retryFactor[_connectionAttempts++ % _retryFactor.Length]);
                    Debug.WriteLine($"Will retry connection in {delay} seconds.");

                    Task.Delay(delay * 1000).Wait();
                    Task.Run(() => EnsureConnection());
                }
            }
        }

        private void EnsureSubscription()
        {
            if (_socket != null && _socket.State == WebSocketState.Open)
            {
                // Subscribe to all CoinKind that doesn't yet have a subscription
                var notYetSubscribed = _subscribed.Where(c => !_subscriptions.Any(s => s == c));

                foreach (var product in notYetSubscribed)
                {
                    Debug.WriteLine($"Subscribing to {product.ProductId}");
                    _socket.SendAsync(GetRequestMessage(RequestType.Subscribe, product), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                // Unsubscribe where subscription still exist
                var notYetUnsubscribed = _subscriptions.Where(c => !_subscribed.Any(s => s == c));

                foreach (var product in notYetUnsubscribed)
                {
                    Debug.WriteLine($"Unsubscribing from {product.ProductId}");
                    _socket.SendAsync(GetRequestMessage(RequestType.Unsubscribe, product), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        private void ReceiveData()
        {
            while (true)
            {
                try
                {
                    var receivedBytes = new ArraySegment<byte>(new byte[1024]);
                    _socket.ReceiveAsync(receivedBytes, _token.Token).Wait();

                    var json = Encoding.UTF8.GetString(receivedBytes.Array).Trim('\0');
                    Debug.WriteLine(json, "Data Received");
                    var response = JsonConvert.DeserializeObject<ResponseMessage>(json);

                    if (response?.Type == ResponseType.Subscriptions)
                    {
                        var tickerChannel = JsonConvert.DeserializeObject<SubscriptionResponse>(json)?.Channels?.FirstOrDefault(c => c.Type == ChannelType.Ticker);
                        if (tickerChannel != null)
                            _subscriptions = tickerChannel.Products;
                    }
                    else if (response?.Type == ResponseType.Ticker)
                    {
                        var ticker = JsonConvert.DeserializeObject<TickerResponse>(json);
                        if (ticker != null)
                        {
                            if (!_lastSequence.ContainsKey(ticker.ProductId))
                                _lastSequence.Add(ticker.ProductId, 0);

                            if (_lastSequence[ticker.ProductId] < ticker.Sequence)
                            {
                                _lastSequence[ticker.ProductId] = ticker.Sequence;

                                PriceUpdated?.Invoke(ticker.ProductId, ticker);
                            }
                        }
                    }

                    if (_token.IsCancellationRequested)
                        break;
                }
                catch (AggregateException ex)
                {
                    _subscriptions.Clear();

                    if (ex.InnerException is TaskCanceledException)
                        break;

                    if (ex.InnerExceptions.Any(e => e is WebSocketException))
                    {
                        EnsureConnection();
                        return;
                    }
                    throw;
                }
            }
        }

        private ArraySegment<byte> GetRequestMessage(RequestType type, params IProduct[] kind)
        {
            var message = new RequestMessage
            {
                Type = type,
                Products = kind.ToList(),
                Channels = new List<Channel> {
                    new Channel { Type = ChannelType.Ticker },
                }
            };

            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message)));
        }

        public void Dispose()
        {
            _token?.Cancel();
            _token?.Dispose();
            _socket?.Dispose();
        }
    }
}