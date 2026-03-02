#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action WebSocket 客户端实现
    /// </summary>
    public class DomainActionWebSocketClient : IDomainActionClient
    {
        private const int RECEIVE_BUFFER_SIZE = 4096;
        private const int MAX_MESSAGE_SIZE = 1024 * 1024; // 1MB
        
        private ClientWebSocket? _webSocket;
        private readonly string _baseUrl;
        private readonly Timer _heartbeatTimer;
        private TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
        private bool _heartbeatEnabled = true;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<DomainActionMessage>> _pendingResponses;
        private readonly ConcurrentQueue<DomainActionMessage> _messageQueue;
        private readonly SemaphoreSlim _sendSemaphore = new(1, 1);
        private Task? _receiveTask;
        private Task? _processTask;
        private CancellationTokenSource? _cancellationTokenSource;
        private DateTime _connectedTime;
        private readonly ConnectionStatistics _statistics = new();
        private string? _sessionId;
        private DomainActionDispatcher? _dispatcher;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        public string? SessionId => _sessionId;
        public DomainActionDispatcher? Dispatcher
        {
            get => _dispatcher;
            set => _dispatcher = value;
        }

        public event EventHandler<DomainActionMessage>? MessageReceived;
        public event EventHandler? Connected;
        public event EventHandler<string>? Disconnected;
        public event EventHandler<Exception>? ErrorOccurred;

        public DomainActionWebSocketClient(string baseUrl)
        {
            _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
            _heartbeatTimer = new Timer(SendHeartbeat, null, Timeout.Infinite, Timeout.Infinite);
            _pendingResponses = new ConcurrentDictionary<string, TaskCompletionSource<DomainActionMessage>>();
            _messageQueue = new ConcurrentQueue<DomainActionMessage>();
        }

        public async Task ConnectAsync(string ticket, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(ticket))
                throw new ArgumentException("Ticket cannot be null or empty", nameof(ticket));

            if (IsConnected)
                throw new InvalidOperationException("Already connected");

            try
            {
                // 构建 WebSocket URL
                string wsUrl = _baseUrl.Replace("http://", "ws://").Replace("https://", "wss://");
                wsUrl += $"/api/v1/ws?ticket={ticket}";

                Logger.Log($"[DomainAction WebSocket] Connecting to {wsUrl}");

                // 创建 WebSocket 连接
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource = new CancellationTokenSource();
                
                await _webSocket.ConnectAsync(new Uri(wsUrl), cancellationToken);

                Logger.Log("[DomainAction WebSocket] Connected successfully");

                _connectedTime = DateTime.UtcNow;
                _sessionId = null; // 等待 CONNECTED 消息设置 session_id

                // 启动接收和处理任务
                _receiveTask = Task.Run(() => ReceiveMessagesAsync(_cancellationTokenSource.Token));
                _processTask = Task.Run(() => ProcessMessagesAsync(_cancellationTokenSource.Token));

                // 启动心跳
                if (_heartbeatEnabled)
                {
                    _heartbeatTimer.Change(TimeSpan.Zero, _heartbeatInterval);
                }

                // 触发连接事件
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Log($"[DomainAction WebSocket] Connection failed: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            // 停止心跳
            _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // 取消所有挂起的操作
            _cancellationTokenSource?.Cancel();

            // 关闭 WebSocket 连接
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DomainAction WebSocket] Error during close: {ex.Message}");
                }
            }

            // 清理资源
            _webSocket?.Dispose();
            _webSocket = null;

            // 取消所有挂起的响应
            foreach (var kvp in _pendingResponses)
            {
                kvp.Value.TrySetCanceled();
            }
            _pendingResponses.Clear();

            // 触发断开连接事件
            Disconnected?.Invoke(this, "Disconnected by client");
        }

        public async Task SendAsync(DomainActionMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!IsConnected)
                throw new InvalidOperationException("Not connected");

            await _sendSemaphore.WaitAsync(cancellationToken);
            try
            {
                // 序列化消息
                string json = JsonSerializer.Serialize(message);
                if (json.Length > MAX_MESSAGE_SIZE)
                    throw new InvalidOperationException($"Message too large: {json.Length} bytes (max: {MAX_MESSAGE_SIZE})");

                byte[] buffer = Encoding.UTF8.GetBytes(json);

                // 发送消息
                await _webSocket!.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);

                // 更新统计信息
                _statistics.MessagesSent++;
                _statistics.LastActivityTime = DateTime.UtcNow;

                if (ClientConfiguration.Instance.EnableBackendDebugLog && message.Domain != Domains.SYSTEM)
                {
                    Logger.Log($"[DomainAction WebSocket] Sent: {message.Domain}:{message.Action}");
                }
            }
            catch (Exception ex)
            {
                _statistics.MessagesFailed++;
                Logger.Log($"[DomainAction WebSocket] Send failed: {ex.Message}");
                ErrorOccurred?.Invoke(this, ex);
                throw;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public Task SendAsync(string domain, string action, object? payload = null, string? targetId = null, CancellationToken cancellationToken = default)
        {
            var message = new DomainActionMessage
            {
                Domain = domain,
                Action = action,
                TargetId = targetId,
                Payload = payload != null ? JsonSerializer.SerializeToElement(payload) : null
            };

            return SendAsync(message, cancellationToken);
        }

        public async Task<TResponse?> SendWithResponseAsync<TResponse>(DomainActionMessage message, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            // 创建响应等待任务
            var tcs = new TaskCompletionSource<DomainActionMessage>();
            _pendingResponses[message.MessageId] = tcs;

            try
            {
                // 发送消息
                await SendAsync(message, cancellationToken);

                // 等待响应（带超时）
                var timeoutTask = Task.Delay(timeout ?? TimeSpan.FromSeconds(30), cancellationToken);
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _pendingResponses.TryRemove(message.MessageId, out _);
                    throw new TimeoutException($"No response received for message {message.MessageId} within timeout");
                }

                var response = await tcs.Task;
                if (response.TryGetPayload<TResponse>(out var responseData))
                {
                    return responseData;
                }

                return default;
            }
            finally
            {
                _pendingResponses.TryRemove(message.MessageId, out _);
            }
        }

        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[RECEIVE_BUFFER_SIZE];
            var messageBuffer = new List<byte>();

            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    var result = await _webSocket!.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Logger.Log("[DomainAction WebSocket] Received close message");
                        await DisconnectAsync();
                        Disconnected?.Invoke(this, "WebSocket closed by server");
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        // 将数据添加到消息缓冲区
                        messageBuffer.AddRange(buffer.Take(result.Count));

                        // 如果消息结束，处理完整消息
                        if (result.EndOfMessage)
                        {
                            string messageText = Encoding.UTF8.GetString(messageBuffer.ToArray());
                            messageBuffer.Clear();

                            // 解析消息
                            var domainMessage = JsonSerializer.Deserialize<DomainActionMessage>(messageText);
                            if (domainMessage != null)
                            {
                                // 更新统计信息
                                _statistics.MessagesReceived++;
                                _statistics.LastActivityTime = DateTime.UtcNow;

                                // 处理特殊消息
                                ProcessSpecialMessage(domainMessage);

                                // 将消息加入处理队列
                                _messageQueue.Enqueue(domainMessage);
                            }
                            else
                            {
                                Logger.Log($"[DomainAction WebSocket] Failed to deserialize message: {messageText.Substring(0, Math.Min(messageText.Length, 200))}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DomainAction WebSocket] Receive error: {ex.Message}");
                    ErrorOccurred?.Invoke(this, ex);
                    
                    // 触发断开连接
                    Disconnected?.Invoke(this, $"Receive error: {ex.Message}");
                    break;
                }
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_messageQueue.TryDequeue(out var message))
                    {
                        await ProcessMessageAsync(message, cancellationToken);
                    }
                    else
                    {
                        // 没有消息时等待一小段时间
                        await Task.Delay(10, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Log($"[DomainAction WebSocket] Process error: {ex.Message}");
                    ErrorOccurred?.Invoke(this, ex);
                }
            }
        }

        private async Task ProcessMessageAsync(DomainActionMessage message, CancellationToken cancellationToken)
        {
            try
            {
                // 检查是否为挂起响应的回复
                if (message.MessageId != null && _pendingResponses.TryGetValue(message.MessageId, out var tcs))
                {
                    tcs.TrySetResult(message);
                }

                // 处理特殊消息（如提取session_id）
                ProcessSpecialMessage(message);

                // 触发消息接收事件
                MessageReceived?.Invoke(this, message);

                // 使用分发器处理消息（如果已设置）
                if (_dispatcher != null)
                {
                    try
                    {
                        await _dispatcher.DispatchAsync(message);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[DomainAction WebSocket] Dispatcher error: {ex.Message}");
                    }
                }

                // 记录调试日志
                if (ClientConfiguration.Instance.EnableBackendDebugLog && message.Domain != Domains.SYSTEM)
                {
                    Logger.Log($"[DomainAction WebSocket] Received: {message.Domain}:{message.Action}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[DomainAction WebSocket] Error processing message: {ex.Message}");
            }
        }

        private void ProcessSpecialMessage(DomainActionMessage message)
        {
            // 处理 CONNECTED 消息，提取 session_id
            if (message.Domain == Domains.SYSTEM && message.Action == Actions.CONNECTED)
            {
                if (message.TryGetPayload<Payloads.ConnectedPayload>(out var connectedPayload))
                {
                    _sessionId = connectedPayload?.SessionId;
                    Logger.Log($"[DomainAction WebSocket] Session ID: {_sessionId}");
                }
            }
            // 处理 PONG 消息，更新统计
            else if (message.Domain == Domains.SYSTEM && message.Action == Actions.PONG)
            {
                _statistics.HeartbeatsReceived++;
            }
            // 处理 ERROR 消息，特殊处理连接错误
            else if (message.Domain == Domains.SYSTEM && message.Action == Actions.ERROR)
            {
                if (message.TryGetPayload<Payloads.ErrorPayload>(out var errorPayload))
                {
                    Logger.Log($"[DomainAction WebSocket] Error: {errorPayload?.Code} - {errorPayload?.Reason}");
                    
                    // 如果是连接错误，可能需要触发重连
                    if (errorPayload != null && ErrorCodes.IsConnectionError(errorPayload.Code))
                    {
                        Disconnected?.Invoke(this, $"Connection error: {errorPayload.Reason}");
                    }
                }
            }
        }

        private async void SendHeartbeat(object? state)
        {
            if (!IsConnected || !_heartbeatEnabled)
                return;

            try
            {
                var heartbeat = new DomainActionMessage
                {
                    Domain = Domains.SYSTEM,
                    Action = Actions.HEARTBEAT,
                    Payload = JsonSerializer.SerializeToElement(new Payloads.HeartbeatPayload
                    {
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        ClientTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    })
                };

                await SendAsync(heartbeat);
                _statistics.HeartbeatsSent++;
            }
            catch (Exception ex)
            {
                Logger.Log($"[DomainAction WebSocket] Heartbeat failed: {ex.Message}");
            }
        }

        public void SetHeartbeatEnabled(bool enabled)
        {
            _heartbeatEnabled = enabled;
            if (enabled && IsConnected)
            {
                _heartbeatTimer.Change(TimeSpan.Zero, _heartbeatInterval);
            }
            else
            {
                _heartbeatTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void SetHeartbeatInterval(TimeSpan interval)
        {
            _heartbeatInterval = interval;
            if (_heartbeatEnabled && IsConnected)
            {
                _heartbeatTimer.Change(TimeSpan.Zero, _heartbeatInterval);
            }
        }

        public ConnectionStatistics GetStatistics()
        {
            _statistics.ConnectionDuration = DateTime.UtcNow - _connectedTime;
            return _statistics;
        }

        public void ResetStatistics()
        {
            _statistics.MessagesSent = 0;
            _statistics.MessagesReceived = 0;
            _statistics.MessagesFailed = 0;
            _statistics.HeartbeatsSent = 0;
            _statistics.HeartbeatsReceived = 0;
            _statistics.ConnectionDuration = TimeSpan.Zero;
            _statistics.LastActivityTime = DateTime.UtcNow;
            _statistics.AverageRoundTripTime = 0;
        }

        public void Dispose()
        {
            _heartbeatTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
            _sendSemaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}