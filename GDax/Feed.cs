using GDax.Enums;
using GDax.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDax
{
    public delegate void PriceUpdatedEventHandler(CoinKind coin, TickerResponse data);

    public interface IFeed
    {
        event PriceUpdatedEventHandler PriceUpdated;

        void Subscribe(CoinKind kind);

        void Unsubscribe(CoinKind kind);
    }

    public class Feed : IFeed, IDisposable
    {
        public event PriceUpdatedEventHandler PriceUpdated;

        private static readonly object _connectionLock = new object();
        private bool _connecting = false;
        private int[] _retryFactor = new int[] { 0, 1, 2, 4, 8 };
        private int _connectionAttempts = 0;
        private List<CoinKind> _subscribed = new List<CoinKind>();
        private List<CoinKind> _subscriptions = new List<CoinKind>();
        private Dictionary<CoinKind, long> _lastSequence = new Dictionary<CoinKind, long>();
        private CancellationTokenSource _token;
        private WebSocket _socket;

        public void Subscribe(CoinKind kind)
        {
            if (_subscribed.Contains(kind))
            {
                EnsureSubscription();
                return;
            }

            _subscribed.Add(kind);
            EnsureConnection();
        }

        public void Unsubscribe(CoinKind kind)
        {
            if (!_subscribed.Contains(kind))
            {
                EnsureSubscription();
                return;
            }

            _subscribed.Remove(kind);
            EnsureConnection();

            if (_lastSequence.ContainsKey(kind))
                _lastSequence.Remove(kind);
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
                Console.WriteLine("Attempting to connect to GDAX API...");
                _socket.ConnectAsync(new Uri("wss://ws-feed.gdax.com"), CancellationToken.None).Wait();
                Console.WriteLine("Connected");

                EnsureSubscription();

                _connectionAttempts = 0;
                Task.Run(() => ReceiveData());
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.Any(e => e is WebSocketException))
                {
                    _subscriptions.Clear();
                    Console.WriteLine($"Failed to connect to GDAX API. Error: {ex.InnerExceptions[0].Message}");
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
                    Console.WriteLine($"Will retry connection in {delay} seconds.");

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

                foreach (var kind in notYetSubscribed)
                {
                    Console.WriteLine($"Subscribing to {kind}");
                    _socket.SendAsync(GetRequestMessage(RequestType.Subscribe, kind), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                // Unsubscribe where subscription still exist
                var notYetUnsubscribed = _subscriptions.Where(c => !_subscribed.Any(s => s == c));

                foreach (var kind in notYetUnsubscribed)
                {
                    Console.WriteLine($"Unsubscribing from {kind}");
                    _socket.SendAsync(GetRequestMessage(RequestType.Unsubscribe, kind), WebSocketMessageType.Text, true, CancellationToken.None);
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

                    var json = Encoding.UTF8.GetString(receivedBytes.Array);
                    var response = JsonConvert.DeserializeObject<ResponseMessage>(json);
                    Console.WriteLine(json);

                    if (response.Type == ResponseType.Subscriptions)
                    {
                        var tickerChannel = JsonConvert.DeserializeObject<SubscriptionResponse>(json)?.Channels?.FirstOrDefault(c => c.Type == ChannelType.Ticker);
                        if (tickerChannel != null)
                            _subscriptions = tickerChannel.Products;
                    }
                    else if (response.Type == ResponseType.Ticker)
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
                    if (ex.InnerException is TaskCanceledException)
                        break;

                    if (ex.InnerExceptions.Any(e => e is WebSocketException))
                    {
                        _subscriptions.Clear();
                        EnsureConnection();
                        return;
                    }
                    throw;
                }
            }
        }

        private ArraySegment<byte> GetRequestMessage(RequestType type, params CoinKind[] kind)
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