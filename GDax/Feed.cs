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
        private List<CoinKind> _subscribed = new List<CoinKind>();
        private Dictionary<CoinKind, long> _lastSequence = new Dictionary<CoinKind, long>();
        private CancellationTokenSource _token;
        private WebSocket _socket;

        public void Subscribe(CoinKind kind)
        {
            if (_subscribed.Contains(kind)) return;

            EnsureConnection();

            _socket.SendAsync(GetRequestMessage(RequestType.Subscribe, kind), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
        }

        public void Unsubscribe(CoinKind kind)
        {
            if (!_subscribed.Contains(kind)) return;

            EnsureConnection();

            _socket.SendAsync(GetRequestMessage(RequestType.Unsubscribe, kind), WebSocketMessageType.Text, true, CancellationToken.None).Wait();

            if (_lastSequence.ContainsKey(kind))
                _lastSequence.Remove(kind);
        }

        private void EnsureConnection()
        {
            if (_socket != null && _socket.State == WebSocketState.Open) return;
            if (_socket != null)
            {
                _token?.Cancel();
                _token?.Dispose();
                _socket.Dispose();
                _socket = null;
            }

            _token = new CancellationTokenSource();
            _socket = SystemClientWebSocket.CreateClientWebSocket();
            _socket.ConnectAsync(new Uri("wss://ws-feed.gdax.com"), CancellationToken.None).Wait();

            // TODO: Resubscribe to existing subscriptions

            Task.Run(() => ReceiveData());
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
                            _subscribed = tickerChannel.Products;
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
