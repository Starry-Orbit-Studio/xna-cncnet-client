#nullable enable
using ClientCore;
using ClientCore.Extensions;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online.EventArguments;
using DTAClient.Online.Backend.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClientCore.Enums;

namespace DTAClient.Online.Backend
{
    public class BackendManager : IConnectionManager
    {
        private readonly BackendSessionManager _sessionManager;
        private readonly BackendSpaceManager _spaceManager;
        private readonly BackendApiClient _apiClient;
        private readonly BackendWebSocketClient _wsClient;
        private readonly WindowManager _windowManager;
        private readonly GameCollection _gameCollection;
        private readonly CnCNetUserData _cncNetUserData;
        private readonly PlayerIdentityService _playerIdentityService;

        private Channel? _mainChannel;
        private readonly List<Channel> _channels = new();
        private readonly List<IRCUser> _userList = new();
        private readonly List<Models.OnlineUserResponse> _pendingOnlineUsers = new();

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

        public Channel? MainChannel
        {
            get => _mainChannel;
            set => _mainChannel = value;
        }
        public List<IRCUser> UserList
        {
            get => _userList;
            set { }
        }

        public bool IsConnected => _sessionManager.IsConnected;
        public bool IsAttemptingConnection => false;
        
        public BackendSpaceManager SpaceManager => _spaceManager;

        public BackendManager(
            WindowManager windowManager,
            GameCollection gameCollection,
            CnCNetUserData cncNetUserData,
            BackendSessionManager sessionManager,
            BackendSpaceManager spaceManager,
            BackendApiClient apiClient,
            BackendWebSocketClient wsClient,
            PlayerIdentityService playerIdentityService)
        {
            _windowManager = windowManager;
            _gameCollection = gameCollection;
            _cncNetUserData = cncNetUserData;
            _sessionManager = sessionManager;
            _spaceManager = spaceManager;
            _apiClient = apiClient;
            _wsClient = wsClient;
            _playerIdentityService = playerIdentityService;

            _sessionManager.SessionCreated += OnSessionCreated;
            _sessionManager.SessionEnded += OnSessionEnded;
            _sessionManager.OnlineUsersReceived += OnOnlineUsersReceived;

            _apiClient.DebugLog += OnDebugLog;
            _wsClient.DebugLog += OnDebugLog;
        }

        private void OnDebugLog(object? sender, string message)
        {
            Logger.Log($"[Backend] {message}");
            
            if (_mainChannel != null && ClientConfiguration.Instance.EnableBackendDebugLog)
            {
                _mainChannel.AddMessage(new ChatMessage(Color.Yellow, message));
            }
        }

        public bool IsCnCNetInitialized()
        {
            return _sessionManager.CurrentSession != null;
        }

        public async Task ConnectAsync()
        {
            try
            {
                await _sessionManager.InitializeAsync();
                await _sessionManager.ConnectToLobbyAsync();
                Connected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Log($"[BackendManager] Connect failed: {ex.Message}");
                ConnectAttemptFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Disconnect()
        {
            _ = _sessionManager.EndSessionAsync();
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public void Connect()
        {
            _ = ConnectAsync();
        }

        public Channel CreateChannel(string uiName, string channelName, bool persistent, bool isChatChannel, string password)
        {
            return new BackendChannel(
                uiName,
                channelName,
                persistent,
                isChatChannel,
                password,
                _apiClient,
                _sessionManager,
                _wsClient,
                _playerIdentityService
            );
        }

        public void AddChannel(Channel channel)
        {
            if (FindChannel(channel.ChannelName) != null)
                throw new ArgumentException("Channel already exists");

            _channels.Add(channel);
        }

        public void RemoveChannel(Channel channel)
        {
            if (channel.Persistent)
                throw new ArgumentException("Persistent channels cannot be removed");

            _channels.Remove(channel);
        }

        public Channel? FindChannel(string channelName)
        {
            return _channels.FirstOrDefault(c => c.ChannelName == channelName);
        }

        public void SetMainChannel(Channel channel)
        {
            _mainChannel = channel;
            Logger.Log($"[BackendManager] Main channel set to {channel.UIName}");
            
            if (_pendingOnlineUsers.Count > 0)
            {
                Logger.Log($"[BackendManager] Adding {_pendingOnlineUsers.Count} pending online users to main channel");
                
                foreach (var onlineUser in _pendingOnlineUsers)
                {
                    string nickname = onlineUser.Nickname ?? $"User_{onlineUser.UserId}";
                    string userId = onlineUser.UserId ?? onlineUser.SessionId;
                    
                    var ircUser = new IRCUser(nickname, userId, userId);
                    ircUser.IsGuest = onlineUser.IsGuest;
                    ircUser.HasVoice = onlineUser.Level > 0;
                    
                    if (!_userList.Any(u => u.Name == ircUser.Name))
                    {
                        _userList.Add(ircUser);
                    }
                    
                    var channelUser = new ChannelUser(ircUser)
                    {
                        IsAdmin = onlineUser.Level > 0,
                        IsFriend = false
                    };
                    
                    _mainChannel.AddUser(channelUser);
                }
                
                _pendingOnlineUsers.Clear();
                Logger.Log($"[BackendManager] Invoking MultipleUsersAdded from SetMainChannel");
                MultipleUsersAdded?.Invoke(this, EventArgs.Empty);
            }
        }

        public void LeaveFromChannel(Channel channel)
        {
            channel.Leave();

            if (!channel.Persistent)
                _channels.Remove(channel);
        }

        public void SendCustomMessage(QueuedMessage qm)
        {
        }

        public void SendWhoIsMessage(string nick)
        {
        }

        public void RemoveChannelFromUser(string userName, string channelName)
        {
            var channel = FindChannel(channelName);
            if (channel != null)
            {
                channel.OnUserLeft(userName);
            }
        }

        public IRCColor[] GetIRCColors()
        {
            return new IRCColor[]
            {
                new IRCColor("Default", false, Color.White, 0),
                new IRCColor("Red", true, Color.Red, 4),
                new IRCColor("Green", true, Color.Green, 3),
                new IRCColor("Blue", true, Color.Blue, 12),
            };
        }

        public bool GetDisconnectStatus()
        {
            return !IsConnected;
        }

        private void OnSessionCreated(object? sender, SessionEventArgs e)
        {
            WelcomeMessageReceived?.Invoke(this, new ServerMessageEventArgs("Connected to backend"));
        }

        private void OnSessionEnded(object? sender, EventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        private void OnOnlineUsersReceived(object? sender, OnlineUsersEventArgs e)
        {
            Logger.Log($"[BackendManager] Received {e.Data.Users.Count} online users, main channel is null: {_mainChannel == null}");
            
            _windowManager.AddCallback(() =>
            {
                if (_mainChannel != null)
                {
                    Logger.Log($"[BackendManager] Adding {e.Data.Users.Count} users to main channel");
                    
                    foreach (var onlineUser in e.Data.Users)
                    {
                        string nickname = onlineUser.Nickname ?? $"User_{onlineUser.UserId}";
                        string userId = onlineUser.UserId ?? onlineUser.SessionId;
                        
                        var ircUser = new IRCUser(nickname, userId, userId);
                        ircUser.IsGuest = onlineUser.IsGuest;
                        ircUser.HasVoice = onlineUser.Level > 0;
                        
                        if (!_userList.Any(u => u.Name == ircUser.Name))
                        {
                            _userList.Add(ircUser);
                        }
                        
                        var channelUser = new ChannelUser(ircUser)
                        {
                            IsAdmin = onlineUser.Level > 0,
                            IsFriend = false
                        };
                        
                        _mainChannel.AddUser(channelUser);
                    }
                    
                    Logger.Log($"[BackendManager] Invoking MultipleUsersAdded event");
                    MultipleUsersAdded?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Logger.Log($"[BackendManager] Main channel not set, storing {e.Data.Users.Count} online users for later");
                    _pendingOnlineUsers.AddRange(e.Data.Users);
                }
            });
        }

        public void OnWelcomeMessageReceived(string message)
        {
            _windowManager.AddCallback(() => WelcomeMessageReceived?.Invoke(this, new ServerMessageEventArgs(message)));
        }

        public void OnGenericServerMessageReceived(string message)
        {
        }

        public void OnAwayMessageReceived(string userName, string reason)
        {
            _windowManager.AddCallback(() => AwayMessageReceived?.Invoke(this, new UserAwayEventArgs(userName, reason)));
        }

        public void OnChannelTopicReceived(string channelName, string topic)
        {
        }

        public void OnChannelTopicChanged(string userName, string channelName, string topic)
        {
        }

        public void OnUserListReceived(string channelName, string[] userList)
        {
        }

        public void OnWhoReplyReceived(string ident, string hostName, string userName, string extraInfo)
        {
            _windowManager.AddCallback(() => WhoReplyReceived?.Invoke(this, new WhoEventArgs(ident, userName, extraInfo)));
        }

        public void OnChannelFull(string channelName)
        {
        }

        public void OnTargetChangeTooFast(string channelName, string message)
        {
        }

        public void OnChannelInviteOnly(string channelName)
        {
        }

        public void OnIncorrectChannelPassword(string channelName)
        {
        }

        public void OnCTCPParsed(string channelName, string userName, string message)
        {
        }

        public void OnNoticeMessageParsed(string notice, string userName)
        {
        }

        public void OnUserJoinedChannel(string channelName, string hostName, string userName, string ident)
        {
        }

        public void OnUserLeftChannel(string channelName, string userName)
        {
        }

        public void OnUserQuitIRC(string userName)
        {
        }

        public void OnChatMessageReceived(string receiver, string senderName, string senderIdent, string message)
        {
        }

        public void OnPrivateMessageReceived(string sender, string message)
        {
            _windowManager.AddCallback(() => PrivateMessageReceived?.Invoke(this, new CnCNetPrivateMessageEventArgs(sender, message)));
        }

        public void OnChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters)
        {
        }

        public void OnUserKicked(string channelName, string userName)
        {
        }

        public void OnErrorReceived(string errorMessage)
        {
        }

        public void OnNameAlreadyInUse()
        {
        }

        public void OnBannedFromChannel(string channelName)
        {
            _windowManager.AddCallback(() => BannedFromChannel?.Invoke(this, new ChannelEventArgs(channelName)));
        }

        public void OnUserNicknameChange(string oldNickname, string newNickname)
        {
        }

        public void OnAttemptedServerChanged(string serverName)
        {
            _windowManager.AddCallback(() => AttemptedServerChanged?.Invoke(this, new AttemptedServerEventArgs(serverName)));
        }

        public void OnConnectAttemptFailed()
        {
            _windowManager.AddCallback(() => ConnectAttemptFailed?.Invoke(this, EventArgs.Empty));
        }

        public void OnConnectionLost(string reason)
        {
            _windowManager.AddCallback(() => ConnectionLost?.Invoke(this, new ConnectionLostEventArgs(reason)));
        }

        public void OnReconnectAttempt()
        {
            _windowManager.AddCallback(() => ReconnectAttempt?.Invoke(this, EventArgs.Empty));
        }

        public void OnDisconnected()
        {
            _windowManager.AddCallback(() => Disconnected?.Invoke(this, EventArgs.Empty));
        }

        public void OnConnected()
        {
            _windowManager.AddCallback(() => Connected?.Invoke(this, EventArgs.Empty));
        }

        public void OnServerLatencyTested(int candidateCount, int closerCount)
        {
        }
    }
}
