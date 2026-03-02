#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online;
using DTAClient.Online.EventArguments;
using DTAClient.Online.Backend;
using DTAClient.Online.Backend.EventArguments;
using DTAClient.Online.Backend.Models;
using DTAClient.Online.RedAlert;
using DTAClient.Online.SharedModels;
using Rampastring.XNAUI;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// 基于 Domain-Action 协议的后端管理器适配器
    /// 实现 IConnectionManager 接口以兼容现有 SOSLobby
    /// </summary>
    public class DomainActionBackendManager : IBackendManager
    {
        private readonly DomainActionWebSocketClient _client;
        private readonly RedAlertApiClient? _apiClient;
        private readonly DomainActionDispatcher _dispatcher;
        private readonly SystemHandler _systemHandler;
        private readonly RoomHandler _roomHandler;
        private readonly ChannelHandler _channelHandler;
        private readonly ModelMapper _modelMapper;
        private readonly Connection _connection;
        private readonly DomainActionSpaceManager _spaceManager;
        private Channel? _mainChannel;
        private bool _isConnected;
        
        internal Connection Connection => _connection;
        private bool _isAttemptingConnection;
        private readonly string? _backendUrl;


        
        public DomainActionBackendManager(string backendUrl, PlayerIdentityService playerIdentityService = null)
        {
            _backendUrl = backendUrl;
            _client = new DomainActionWebSocketClient(backendUrl);
            _dispatcher = new DomainActionDispatcher();
            
            _systemHandler = new SystemHandler();
            _roomHandler = new RoomHandler();
            _channelHandler = new ChannelHandler();
            _modelMapper = new ModelMapper();
            
            _dispatcher.RegisterHandler(_systemHandler);
            _dispatcher.RegisterHandler(_roomHandler);
            _dispatcher.RegisterHandler(_channelHandler);
            
            _client.Dispatcher = _dispatcher;
            
            // 创建 Connection 适配器以兼容旧的 Channel 构造函数
            _connection = new Connection(this, new Random(), playerIdentityService);
            
            // 初始化空间管理器
            _spaceManager = new DomainActionSpaceManager(this, null, playerIdentityService);
            
            SetupEventHandlers();
        }
        
        public DomainActionBackendManager(RedAlertApiClient apiClient, PlayerIdentityService? playerIdentityService = null)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _client = new DomainActionWebSocketClient(GetWebSocketBaseUrl(apiClient.BaseUrl));
            _dispatcher = new DomainActionDispatcher();
            
            _systemHandler = new SystemHandler();
            _roomHandler = new RoomHandler();
            _channelHandler = new ChannelHandler();
            _modelMapper = new ModelMapper();
            
            _dispatcher.RegisterHandler(_systemHandler);
            _dispatcher.RegisterHandler(_roomHandler);
            _dispatcher.RegisterHandler(_channelHandler);
            
            _client.Dispatcher = _dispatcher;
            
            // 创建 Connection 适配器以兼容旧的 Channel 构造函数
            _connection = new Connection(this, new Random(), playerIdentityService);
            
            // 初始化空间管理器，传递 API 客户端和玩家身份服务
            _spaceManager = new DomainActionSpaceManager(this, apiClient, playerIdentityService);
            
            SetupEventHandlers();
        }
        
        private string GetWebSocketBaseUrl(string baseUrl)
        {
            // 从HTTP URL转换为WebSocket URL（去除路径部分，保留协议和主机）
            var uri = new Uri(baseUrl);
            return $"{uri.Scheme}://{uri.Host}:{uri.Port}";
        }
        
        private void SetupEventHandlers()
        {
            _systemHandler.Connected += (sender, payload) =>
            {
                _isConnected = true;
                _isAttemptingConnection = false;
                
                // 创建默认主频道
                var mainChannel = CreateChannel("Main Lobby", "main-lobby", true, true, "");
                SetMainChannel(mainChannel);
                
                Connected?.Invoke(this, EventArgs.Empty);
                Logger.Log($"[DomainActionBackendManager] Connected with session: {payload.SessionId}, main channel created");
            };
            
            _systemHandler.ErrorReceived += (sender, payload) =>
            {
                Logger.Log($"[DomainActionBackendManager] Error: {payload.Code} - {payload.Reason}");
                
                // 如果是连接错误，触发 ConnectionLost
                if (ErrorCodes.IsConnectionError(payload.Code))
                {
                    ConnectionLost?.Invoke(this, new ConnectionLostEventArgs(payload.Reason));
                }
            };
            
            _client.Disconnected += (sender, reason) =>
            {
                _isConnected = false;
                Disconnected?.Invoke(this, EventArgs.Empty);
                Logger.Log($"[DomainActionBackendManager] Disconnected: {reason}");
            };
            
            _client.ErrorOccurred += (sender, ex) =>
            {
                Logger.Log($"[DomainActionBackendManager] Client error: {ex.Message}");
            };
            
            // 频道相关事件转发
            _channelHandler.ChannelJoined += (sender, payload) =>
            {
                // TODO: 转换为 IRC 风格的 UserListReceived 等事件
                Logger.Log($"[DomainActionBackendManager] Channel joined: {payload.ChannelId}");
            };
            
            _channelHandler.NewChatReceived += (sender, payload) =>
            {
                // 转换为 ChatMessageReceived 事件
                // 需要映射到合适的频道
                Logger.Log($"[DomainActionBackendManager] New chat from {payload.Sender.UserId}: {payload.Content}");
            };
            
            _channelHandler.MemberChanged += (sender, payload) =>
            {
                // 转换为 UserJoinedChannel 或 UserLeftChannel 事件
                Logger.Log($"[DomainActionBackendManager] Member changed: {payload.User.UserId}, action: {payload.Action}");
            };
            
            // 房间相关事件转发
            _roomHandler.RoomCreated += (sender, payload) =>
            {
                Logger.Log($"[DomainActionBackendManager] Room created: {payload.RoomId}");
                // TODO: 转换为 RoomCreatedEventArgs
                // 暂时不触发事件，避免需要创建 RoomCreatedEventData
                // RoomCreated?.Invoke(this, new RoomCreatedEventArgs(new RoomCreatedEventData()));
            };
            
            _roomHandler.RoomSynced += (sender, payload) =>
            {
                Logger.Log($"[DomainActionBackendManager] Room synced: {payload.RoomId}");
                // TODO: 转换为 RoomUpdatedEventArgs
            };
        }
        
        #region IConnectionManager 实现 - 基本属性
        
        public Channel MainChannel
        {
            get => _mainChannel!;
            set => _mainChannel = value;
        }
        
        public bool IsConnected => _isConnected;
        
        public bool IsAttemptingConnection => _isAttemptingConnection;
        
        public List<IRCUser> UserList { get; } = new List<IRCUser>();
        
        public BackendSpaceManager SpaceManager => _spaceManager;
        
        #endregion
        
        #region IConnectionManager 实现 - 事件
        
        public event EventHandler<ServerMessageEventArgs>? WelcomeMessageReceived;
        public event EventHandler<UserAwayEventArgs>? AwayMessageReceived;
        public event EventHandler<WhoEventArgs>? WhoReplyReceived;
        public event EventHandler<CnCNetPrivateMessageEventArgs>? PrivateMessageReceived;
        public event EventHandler<PrivateCTCPEventArgs>? PrivateCTCPReceived;
        public event EventHandler<ChannelEventArgs>? BannedFromChannel;
        public event EventHandler<AttemptedServerEventArgs>? AttemptedServerChanged;
        public event EventHandler? ConnectAttemptFailed;
        public event EventHandler<ConnectionLostEventArgs>? ConnectionLost;
        public event EventHandler? ReconnectAttempt;
        public event EventHandler? Disconnected;
        public event EventHandler? Connected;
        public event EventHandler<UserEventArgs>? UserAdded;
        public event EventHandler<UserEventArgs>? UserGameIndexUpdated;
        public event EventHandler<UserNameIndexEventArgs>? UserRemoved;
        public event EventHandler? MultipleUsersAdded;
        
        // BackendManager 特定事件
        public event EventHandler<RoomCreatedEventArgs>? RoomCreated;
        public event EventHandler<RoomUpdatedEventArgs>? RoomUpdated;
        public event EventHandler<RoomDeletedEventArgs>? RoomDeleted;
        
        #endregion
        
        #region IConnectionManager 实现 - 连接管理
        
        public void Connect()
        {
            if (_isConnected || _isAttemptingConnection)
                return;
                
            _isAttemptingConnection = true;
            AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs("DomainActionBackend"));
            
            // 启动异步连接流程
            _ = ConnectInternalAsync();
        }
        
        public async Task ConnectAsync()
        {
            if (_isConnected || _isAttemptingConnection)
                return;
                
            _isAttemptingConnection = true;
            AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs("DomainActionBackend"));
            
            try
            {
                await ConnectInternalAsync();
            }
            catch
            {
                _isAttemptingConnection = false;
                throw;
            }
        }
        
        private async Task ConnectInternalAsync()
        {
            try
            {
                // 获取连接票据
                string ticket;
                if (_apiClient != null)
                {
                    // 使用 RedAlertApiClient 获取票据
                    var ticketResponse = await _apiClient.GetConnectTicketAsync();
                    ticket = ticketResponse.WsTicket;
                }
                else if (_backendUrl != null)
                {
                    // 旧模式：需要外部提供票据，这里无法自动获取
                    Logger.Log("[DomainActionBackendManager] Cannot auto-connect: need ticket but no API client available");
                    _isAttemptingConnection = false;
                    ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);
                    return;
                }
                else
                {
                    Logger.Log("[DomainActionBackendManager] Cannot auto-connect: no backend URL or API client available");
                    _isAttemptingConnection = false;
                    ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);
                    return;
                }
                
                // 使用票据连接 WebSocket
                await ConnectAsync(ticket);
            }
            catch (Exception ex)
            {
                _isAttemptingConnection = false;
                ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);
                Logger.Log($"[DomainActionBackendManager] Auto-connect failed: {ex.Message}");
            }
        }
        
        public async Task ConnectAsync(string ticket)
        {
            if (_isConnected || _isAttemptingConnection)
                return;
                
            _isAttemptingConnection = true;
            AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs("DomainActionBackend"));
            
            try
            {
                await _client.ConnectAsync(ticket);
                // 连接成功事件由 _systemHandler.Connected 触发
            }
            catch (Exception ex)
            {
                _isAttemptingConnection = false;
                ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);
                Logger.Log($"[DomainActionBackendManager] Connection failed: {ex.Message}");
                throw;
            }
        }
        
        public void Disconnect()
        {
            _ = _client.DisconnectAsync();
        }
        
        public bool GetDisconnectStatus()
        {
            return !_isConnected;
        }
        
        #endregion
        
        #region IConnectionManager 实现 - 频道管理
        
        public bool IsCnCNetInitialized()
        {
            return _isConnected;
        }
        
        public Channel CreateChannel(string uiName, string channelName, bool persistent, bool isChatChannel, string password)
        {
            // 创建本地 Channel 对象
            var channel = new Channel(uiName, channelName, persistent, isChatChannel, password, _connection);
            return channel;
        }
        
        public void AddChannel(Channel channel)
        {
            // 加入 Domain-Action 频道
            var joinMessage = new DomainActionMessage
            {
                Domain = Domains.CHANNEL,
                Action = Actions.JOIN_CHANNEL,
                TargetId = channel.ChannelName,
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    channel_id = channel.ChannelName
                })
            };
            
            _ = _client.SendAsync(joinMessage);
            Logger.Log($"[DomainActionBackendManager] Joining channel: {channel.ChannelName}");
        }
        
        public void RemoveChannel(Channel channel)
        {
            // 离开 Domain-Action 频道
            // TODO: 发送离开频道消息
            Logger.Log($"[DomainActionBackendManager] Leaving channel: {channel.ChannelName}");
        }
        
        public IRCColor[] GetIRCColors()
        {
            // 返回默认 IRC 颜色
            return new IRCColor[0];
        }
        
        public void LeaveFromChannel(Channel channel)
        {
            RemoveChannel(channel);
        }
        
        public void SetMainChannel(Channel channel)
        {
            _mainChannel = channel;
            Logger.Log($"[DomainActionBackendManager] Main channel set to {channel.UIName}");
            MultipleUsersAdded?.Invoke(this, EventArgs.Empty);
        }
        
        public Channel? FindChannel(string channelName)
        {
            // 简化实现：只返回主频道
            if (_mainChannel?.ChannelName == channelName)
                return _mainChannel;
            return null;
        }
        
        #endregion
        
        #region IConnectionManager 实现 - 消息发送
        
        public void SendCustomMessage(QueuedMessage qm)
        {
            // 将 QueuedMessage 转换为 Domain-Action 消息
            // 这里需要根据消息类型进行转换
            Logger.Log($"[DomainActionBackendManager] SendCustomMessage: {qm.Command}");
            
            // 如果是聊天消息
            if (qm.Command.StartsWith("PRIVMSG"))
            {
                // 解析 PRIVMSG #channel :message
                // 转换为 SEND_CHAT 动作
            }
        }
        
        public void SendWhoIsMessage(string nick)
        {
            // Domain-Action 协议可能通过 USER_FULL_CARD 获取用户信息
            Logger.Log($"[DomainActionBackendManager] SendWhoIsMessage for: {nick}");
        }
        
        public void RemoveChannelFromUser(string userName, string channelName)
        {
            // 用户离开频道
            Logger.Log($"[DomainActionBackendManager] RemoveChannelFromUser: {userName} from {channelName}");
        }
        
        #endregion
        
        #region IConnectionManager 实现 - IRC 事件处理（空实现或转发）
        
        // 以下方法是 IRC 协议特定的事件处理，对于 Domain-Action 协议大多数不适用
        // 我们提供空实现或记录日志
        
        public void OnWelcomeMessageReceived(string message)
        {
            Logger.Log($"[DomainActionBackendManager] OnWelcomeMessageReceived: {message}");
        }
        
        public void OnGenericServerMessageReceived(string message)
        {
            Logger.Log($"[DomainActionBackendManager] OnGenericServerMessageReceived: {message}");
        }
        
        public void OnAwayMessageReceived(string userName, string reason)
        {
            Logger.Log($"[DomainActionBackendManager] OnAwayMessageReceived: {userName} - {reason}");
        }
        
        public void OnChannelTopicReceived(string channelName, string topic)
        {
            Logger.Log($"[DomainActionBackendManager] OnChannelTopicReceived: {channelName} - {topic}");
        }
        
        public void OnChannelTopicChanged(string userName, string channelName, string topic)
        {
            Logger.Log($"[DomainActionBackendManager] OnChannelTopicChanged: {userName} {channelName} - {topic}");
        }
        
        public void OnUserListReceived(string channelName, string[] userList)
        {
            Logger.Log($"[DomainActionBackendManager] OnUserListReceived: {channelName} - {userList.Length} users");
        }
        
        public void OnWhoReplyReceived(string ident, string hostName, string userName, string extraInfo)
        {
            Logger.Log($"[DomainActionBackendManager] OnWhoReplyReceived: {userName}");
        }
        
        public void OnChannelFull(string channelName)
        {
            Logger.Log($"[DomainActionBackendManager] OnChannelFull: {channelName}");
        }
        
        public void OnTargetChangeTooFast(string channelName, string message)
        {
            Logger.Log($"[DomainActionBackendManager] OnTargetChangeTooFast: {channelName} - {message}");
        }
        
        public void OnChannelInviteOnly(string channelName)
        {
            Logger.Log($"[DomainActionBackendManager] OnChannelInviteOnly: {channelName}");
        }
        
        public void OnIncorrectChannelPassword(string channelName)
        {
            Logger.Log($"[DomainActionBackendManager] OnIncorrectChannelPassword: {channelName}");
        }
        
        public void OnCTCPParsed(string channelName, string userName, string message)
        {
            Logger.Log($"[DomainActionBackendManager] OnCTCPParsed: {channelName} {userName} - {message}");
        }
        
        public void OnNoticeMessageParsed(string notice, string userName)
        {
            Logger.Log($"[DomainActionBackendManager] OnNoticeMessageParsed: {userName} - {notice}");
        }
        
        public void OnUserJoinedChannel(string channelName, string hostName, string userName, string ident)
        {
            Logger.Log($"[DomainActionBackendManager] OnUserJoinedChannel: {userName} to {channelName}");
        }
        
        public void OnUserLeftChannel(string channelName, string userName)
        {
            Logger.Log($"[DomainActionBackendManager] OnUserLeftChannel: {userName} from {channelName}");
        }
        
        public void OnUserQuitIRC(string userName)
        {
            Logger.Log($"[DomainActionBackendManager] OnUserQuitIRC: {userName}");
        }
        
        public void OnChatMessageReceived(string receiver, string senderName, string senderIdent, string message)
        {
            Logger.Log($"[DomainActionBackendManager] OnChatMessageReceived: {senderName} to {receiver} - {message}");
        }
        
        public void OnPrivateMessageReceived(string sender, string message)
        {
            Logger.Log($"[DomainActionBackendManager] OnPrivateMessageReceived: {sender} - {message}");
        }
        
        public void OnChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters)
        {
            Logger.Log($"[DomainActionBackendManager] OnChannelModesChanged: {userName} {channelName} - {modeString}");
        }
        
        public void OnUserKicked(string channelName, string userName)
        {
            Logger.Log($"[DomainActionBackendManager] OnUserKicked: {userName} from {channelName}");
        }
        
        public void OnErrorReceived(string errorMessage)
        {
            Logger.Log($"[DomainActionBackendManager] OnErrorReceived: {errorMessage}");
        }
        
        public void OnNameAlreadyInUse()
        {
            Logger.Log("[DomainActionBackendManager] OnNameAlreadyInUse");
        }
        
        public void OnBannedFromChannel(string channelName)
        {
            Logger.Log($"[DomainActionBackendManager] OnBannedFromChannel: {channelName}");
        }
        
        public void OnUserNicknameChange(string oldNickname, string newNickname)
        {
            Logger.Log($"[DomainActionBackendManager] OnUserNicknameChange: {oldNickname} -> {newNickname}");
        }
        
        public void OnAttemptedServerChanged(string serverName)
        {
            Logger.Log($"[DomainActionBackendManager] OnAttemptedServerChanged: {serverName}");
        }
        
        public void OnConnectAttemptFailed()
        {
            Logger.Log("[DomainActionBackendManager] OnConnectAttemptFailed");
        }
        
        public void OnConnectionLost(string reason)
        {
            Logger.Log($"[DomainActionBackendManager] OnConnectionLost: {reason}");
        }
        
        public void OnReconnectAttempt()
        {
            Logger.Log("[DomainActionBackendManager] OnReconnectAttempt");
        }
        
        public void OnDisconnected()
        {
            Logger.Log("[DomainActionBackendManager] OnDisconnected");
        }
        
        public void OnConnected()
        {
            Logger.Log("[DomainActionBackendManager] OnConnected");
        }
        
        public void OnServerLatencyTested(int candidateCount, int closerCount)
        {
            Logger.Log($"[DomainActionBackendManager] OnServerLatencyTested: {candidateCount} candidates, {closerCount} closer");
        }
        

        
        #endregion
        
        #region 特定于 Domain-Action 的方法
        
        /// <summary>
        /// 获取底层 DomainAction 客户端
        /// </summary>
        public IDomainActionClient GetDomainActionClient()
        {
            return _client;
        }
        
        /// <summary>
        /// 发送 Domain-Action 消息
        /// </summary>
        public async Task SendDomainActionMessageAsync(DomainActionMessage message)
        {
            await _client.SendAsync(message);
        }
        
        /// <summary>
        /// 发送 Domain-Action 消息并等待响应
        /// </summary>
        public async Task<TResponse?> SendDomainActionMessageWithResponseAsync<TResponse>(
            DomainActionMessage message, 
            TimeSpan? timeout = null)
        {
            return await _client.SendWithResponseAsync<TResponse>(message, timeout);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Domain-Action 协议的空间管理器适配器
    /// </summary>
    internal class DomainActionSpaceManager : BackendSpaceManager
    {
        private readonly DomainActionBackendManager _backendManager;
        private readonly RedAlertApiClient? _apiClient;
        private readonly PlayerIdentityService? _playerIdentityService;
        
        public DomainActionSpaceManager(DomainActionBackendManager backendManager, RedAlertApiClient? apiClient, PlayerIdentityService? playerIdentityService = null) 
            : base(null!, null!, null!, null!) // 传递空依赖，我们将重写所有方法
        {
            _backendManager = backendManager;
            _apiClient = apiClient;
            _playerIdentityService = playerIdentityService;
        }
        
        public override async Task<BackendChannel> CreateLobbyAsync(string name, int maxMembers = 100, bool isPrivate = false)
        {
            throw new NotImplementedException("CreateLobbyAsync is not implemented for DomainActionSpaceManager");
        }
        
        public override async Task<BackendChannel> CreateRoomAsync(string name, int maxMembers, bool isPrivate = false)
        {
            // 使用 Domain-Action 协议创建房间
            Logger.Log($"[DomainActionSpaceManager] CreateRoomAsync: {name}, maxMembers: {maxMembers}, isPrivate: {isPrivate}");
            
            // TODO: 实现真正的 Domain-Action 协议房间创建
            // 暂时创建虚拟通道
            var channel = new Channel(
                name,
                $"room-{Guid.NewGuid()}",
                false,
                false,
                "",
                _backendManager.Connection
            );
            
            // 将 Channel 包装为 BackendChannel 适配器
            return new DomainActionBackendChannel(channel);
        }
        
        public override async Task<List<BackendChannel>> GetLobbiesAsync()
        {
            throw new NotImplementedException("GetLobbiesAsync is not implemented for DomainActionSpaceManager");
        }
        
        public override async Task<List<BackendChannel>> GetRoomsAsync()
        {
            // 返回空列表，待实现
            return new List<BackendChannel>();
        }
        
        public override async Task<List<SpaceResponse>> GetRoomSpacesAsync()
        {
            Logger.Log("[DomainActionSpaceManager] GetRoomSpacesAsync called");
            
            // 如果没有 API 客户端，返回空列表
            if (_apiClient == null)
            {
                Logger.Log("[DomainActionSpaceManager] No API client available, returning empty list");
                return new List<SpaceResponse>();
            }
            
            try
            {
                // 检查访问令牌，如果 API 客户端没有令牌，尝试从 PlayerIdentityService 获取
                if (string.IsNullOrEmpty(_apiClient.AccessToken) && _playerIdentityService != null)
                {
                    var accessToken = _playerIdentityService.GetAccessToken();
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        Logger.Log("[DomainActionSpaceManager] Setting access token from PlayerIdentityService");
                        _apiClient.SetAccessToken(accessToken);
                    }
                    else
                    {
                        Logger.Log("[DomainActionSpaceManager] No access token available from PlayerIdentityService");
                    }
                }
                
                // 获取频道列表
                Logger.Log("[DomainActionSpaceManager] Fetching channels from RedAlert API");
                var channels = await _apiClient.GetChannelsAsync();
                Logger.Log($"[DomainActionSpaceManager] Retrieved {channels.Count} channels");
                
                var spaces = new List<SpaceResponse>();
                
                foreach (var channel in channels)
                {
                    try
                    {
                        // 获取频道中的房间
                        string channelId = channel.GetChannelId();
                        Logger.Log($"[DomainActionSpaceManager] Fetching rooms for channel {channelId} ({channel.Name})");
                        var rooms = await _apiClient.GetChannelRoomsAsync(channelId);
                        
                        foreach (var room in rooms)
                        {
                            // 构建包含地图和游戏模式信息的房间名称
                            string roomName = room.Settings.Name;
                            if (!string.IsNullOrEmpty(room.Settings.MapName))
                            {
                                roomName += $" [{room.Settings.MapName}]";
                            }
                            if (!string.IsNullOrEmpty(room.Settings.GameMode) && room.Settings.GameMode != "standard")
                            {
                                roomName += $" ({room.Settings.GameMode})";
                            }
                            
                            // 为房间生成一个稳定的整数ID（使用RoomId的哈希码）
                            int roomId = Math.Abs(room.RoomId.GetHashCode());
                            
                            var spaceResponse = new SpaceResponse
                            {
                                Id = roomId,
                                Name = roomName,
                                MaxMembers = room.Settings.MaxPlayers,
                                MemberCount = room.Members.Count,
                                IsPrivate = room.Settings.IsPrivate,
                                Status = room.State,
                                OwnerUserId = int.TryParse(room.HostId, out int hostId) ? hostId : null
                            };
                            
                            spaces.Add(spaceResponse);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[DomainActionSpaceManager] Error fetching rooms for channel {channel.Name}: {ex.Message}");
                    }
                }
                
                Logger.Log($"[DomainActionSpaceManager] Returning {spaces.Count} real rooms");
                return spaces;
            }
            catch (Exception ex)
            {
                Logger.Log($"[DomainActionSpaceManager] Error in GetRoomSpacesAsync: {ex.Message}");
                Logger.Log($"[DomainActionSpaceManager] Stack trace: {ex.StackTrace}");
                
                // 出错时返回空列表，避免影响 UI
                return new List<SpaceResponse>();
            }
        }
        
        public override async Task<List<SpaceMemberResponse>> GetSpaceMembersAsync(int spaceId)
        {
            throw new NotImplementedException("GetSpaceMembersAsync is not implemented for DomainActionSpaceManager");
        }
        
        public override async Task<BackendChannel?> GetChannelAsync(int spaceId)
        {
            throw new NotImplementedException("GetChannelAsync is not implemented for DomainActionSpaceManager");
        }
        
        public override async Task JoinChannelAsync(int spaceId)
        {
            throw new NotImplementedException("JoinChannelAsync is not implemented for DomainActionSpaceManager");
        }
        
        public override async Task LeaveChannelAsync(int spaceId)
        {
            throw new NotImplementedException("LeaveChannelAsync is not implemented for DomainActionSpaceManager");
        }
    }
    
    /// <summary>
    /// Domain-Action 协议的 BackendChannel 适配器
    /// </summary>
    internal class DomainActionBackendChannel : BackendChannel
    {
        private readonly Channel _wrappedChannel;
        
        public DomainActionBackendChannel(Channel channel)
            : base(channel.UIName, channel.ChannelName, channel.Persistent, 
                  channel.IsChatChannel, channel.Password, null!, null!, null!, null!)
        {
            _wrappedChannel = channel;
        }
        
        public override void SendChatMessage(string message, IRCColor color)
        {
            // 转发到包装的通道
            _wrappedChannel.SendChatMessage(message, color);
        }
        
        // 重写其他可能需要的方法以避免 NullReferenceException
        public override void SendChatMessageBackend(string message, IRCColor color)
        {
            _wrappedChannel.SendChatMessage(message, color);
        }
        
        public override void JoinBackend()
        {
            // 无操作
        }
        
        public override void LeaveBackend()
        {
            // 无操作
        }
        
        public override Task LoadMembersAsync()
        {
            return Task.CompletedTask;
        }
    }
}