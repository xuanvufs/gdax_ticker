using GDax.Enums;
using GDax.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GDax
{
    public delegate void PriceUpdatedEventHandler(IProduct product, TickerResponse data);
    public delegate void ConnectionStateEventHandler(WebSocketState state);

    public interface IFeed
    {
        event PriceUpdatedEventHandler PriceUpdated;
        event ConnectionStateEventHandler ConnectionStateChanged;

        void Subscribe(IProduct product);

        void Unsubscribe(IProduct product);

        void Stop();
    }

    public class Feed : IFeed, IDisposable
    {
        public event PriceUpdatedEventHandler PriceUpdated;
        public event ConnectionStateEventHandler ConnectionStateChanged;

        private static readonly object _lock = new object();
        private readonly CancellationTokenSource _token;
        private readonly List<IProduct> _subscription = new List<IProduct>();
        private readonly List<IProduct> _subscriptionPending = new List<IProduct>();
        private readonly Dictionary<IProduct, long> _lastSequence = new Dictionary<IProduct, long>();
        private readonly BlockingCollection<TickerResponse> _responses = new BlockingCollection<TickerResponse>();
        private readonly Task _task;

        private WebSocket _socket;
        private WebSocketState _socketState;
        private int _connectionAttempts;

        public Feed()
        {
            _token = new CancellationTokenSource();
            _task = Task.Run(Producer);
            Task.Run(() => Consume());
        }

        public void Subscribe(IProduct product)
        {
            if (_subscription.Contains(product))
            {
                return;
            }

            _subscription.Add(product);

            lock (_lock)
            {
                Monitor.Pulse(_lock);
            }
        }

        public void Unsubscribe(IProduct product)
        {
            if (!_subscription.Contains(product))
            {
                return;
            }

            _subscription.Remove(product);
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

            var json = JsonConvert.SerializeObject(message);
            Debug.WriteLine(json, "Request Message");

            return new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
        }

        private RequestMessage CreateRequest(RequestType type, params IProduct[] products)
        {
            return new RequestMessage
            {
                Type = type,
                Products = products.ToList(),
                Channels = new List<Channel>
                {
                    new Channel {Type = ChannelType.Ticker }
                }
            };
        }

        private async Task<bool> Connect()
        {
            try
            {
                if (_socket != null && _socket.State == WebSocketState.Open)
                    return true;

                if (_socket?.State == WebSocketState.Connecting)
                    return false;

                _socket?.Dispose();
                _lastSequence.Clear();
                Debug.WriteLine("Connecting to wss://ws-feed.gdax.com", "Trace");
                _socket = await SystemClientWebSocket.ConnectAsync(new Uri("wss://ws-feed.gdax.com"), _token.Token);

                return _socket.State == WebSocketState.Open;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to connect to GDAX API.{Environment.NewLine}{ex}", "Error");
                return false;
            }
            finally
            {
                if (_socketState != _socket.State)
                {
                    _socketState = _socket.State;
                    ConnectionStateChanged?.BeginInvoke(_socketState, ConnectionStateChangedCallback, null);
                }
            }
        }

        private void ConnectionStateChangedCallback(IAsyncResult result)
        {
            try
            {
                ConnectionStateChanged?.EndInvoke(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConnectionChangedEventHandler caused the exception.{Environment.NewLine}{ex}", "Error");
            }
        }

        private async Task Producer()
        {
            while (true)
            {
                try
                {

                    if (_token.IsCancellationRequested)
                        break;

                    if (!_subscription.Any() && !_lastSequence.Any())
                    {
                        lock (_lock)
                        {
                            Monitor.Wait(_lock);
                            continue;
                        }
                    }

                    var isConnected = await Connect();
                    if (!isConnected)
                    {
                        var delay = Math.Min(120, 1 << _connectionAttempts++);
                        await Task.Delay(delay * 1000, _token.Token);
                        continue;
                    }
                    _connectionAttempts = 0;

                    var subscribed = _lastSequence.Keys.ToList();
                    var notYetSubscribed = _subscription.Except(subscribed.Union(_subscriptionPending)).ToList();
                    if (notYetSubscribed.Any())
                    {
                        await _socket.SendAsync(GetRequestMessage(RequestType.Subscribe, notYetSubscribed.ToArray()), WebSocketMessageType.Text, true, _token.Token);
                        _subscriptionPending.AddRange(notYetSubscribed);
                    }

                    var notYetUnsubscribed = subscribed.Except(_subscription.Union(_subscriptionPending)).ToList();
                    if (notYetUnsubscribed.Any())
                        await _socket.SendAsync(GetRequestMessage(RequestType.Unsubscribe, notYetUnsubscribed.ToArray()), WebSocketMessageType.Text, true, _token.Token);

                    byte[] receivedBytes = null;
                    using (var stream = new MemoryStream())
                    {
                        var buffer = new ArraySegment<byte>(new byte[512]);
                        WebSocketReceiveResult result;
                        do
                        {
                            Debug.WriteLine("Preparing to receive data.", "Trace");
                            result = await _socket.ReceiveAsync(buffer, _token.Token);
                            Debug.WriteLine(result.Count, "Bytes Received");
                            stream.Write(buffer.Array, 0, result.Count);
                        } while (!result.EndOfMessage);

                        receivedBytes = stream.ToArray();
                    }

                    var json = Encoding.UTF8.GetString(receivedBytes.SkipWhile(b => b == 0).TakeWhile(b => b != 0).ToArray());
                    Debug.WriteLine(json, "Data Received");

                    var response = JsonConvert.DeserializeObject<ResponseMessage>(json);
                    if (response?.Type == ResponseType.Subscriptions)
                    {
                        var tickerChannel = JsonConvert.DeserializeObject<SubscriptionResponse>(json)?.Channels?.FirstOrDefault(c => c.Type == ChannelType.Ticker);
                        if (tickerChannel != null)
                        {
                            _lastSequence.Clear();
                            foreach (var product in tickerChannel.Products)
                            {
                                _subscriptionPending.Remove(product);
                                _lastSequence.Add(product, 0);
                            }

                            if (!_lastSequence.Any())
                            {
                                Debug.WriteLine("No more subscriptions, disconnecting.", "Trace");
                                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "No more subscriptions", _token.Token);
                            }
                        }
                    }
                    else if (response?.Type == ResponseType.Ticker)
                    {
                        var ticker = JsonConvert.DeserializeObject<TickerResponse>(json);
                        if (ticker != null)
                        {
                            _responses.Add(ticker);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (WebSocketException ex)
                {
                    Debug.WriteLine(ex.ToString(), "Error");
                    if (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                        _socket.Abort();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString(), "Error");
                }
            }
        }

        private void Consume()
        {
            while (true)
            {
                try
                {
                    if (_token.IsCancellationRequested)
                        break;

                    var ticker = _responses.Take(_token.Token);

                    if (!_lastSequence.ContainsKey(ticker.ProductId))
                    {
                        _responses.Add(ticker);
                        Thread.Sleep(500);
                        continue;
                    }

                    if (_lastSequence[ticker.ProductId] < ticker.Sequence)
                    {
                        _lastSequence[ticker.ProductId] = ticker.Sequence;

                        PriceUpdated?.Invoke(ticker.ProductId, ticker);
                    }
                } catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void Stop()
        {
            _token.Cancel();
            lock (_lock)
            {
                Monitor.Pulse(_lock);
            }
        }

        public void Dispose()
        {
            _responses?.Dispose();
            _token?.Dispose();
            _socket?.Dispose();
        }
    }
}