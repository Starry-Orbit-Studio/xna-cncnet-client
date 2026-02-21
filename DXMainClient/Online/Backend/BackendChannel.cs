#nullable enable
using ClientCore;
using ClientCore.Enums;
using DTAClient.Online.EventArguments;
using DTAClient.DXGUI;
using ClientCore.Extensions;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DTAClient.Online.Backend.Models;
using DTAClient;

namespace DTAClient.Online.Backend
{
    public class BackendChannel : Channel
    {
        private readonly BackendApiClient _apiClient;
        private readonly BackendSessionManager _sessionManager;
        private readonly BackendWebSocketClient _wsClient;
        private readonly PlayerIdentityService _playerIdentityService;
        private int _spaceId;
        private string? _channel;

        public int SpaceId => _spaceId;

        public new string ChannelName => _channel ?? base.ChannelName;

        public BackendChannel(
            string uiName,
            string channelName,
            bool persistent,
            bool isChatChannel,
            string? password,
            BackendApiClient apiClient,
            BackendSessionManager sessionManager,
            BackendWebSocketClient wsClient,
            PlayerIdentityService playerIdentityService)
            : base(uiName, channelName, persistent, isChatChannel, password ?? string.Empty, new NullConnection(playerIdentityService))
        {
            _apiClient = apiClient;
            _sessionManager = sessionManager;
            _wsClient = wsClient;
            _playerIdentityService = playerIdentityService;
        }

        public override void SendChatMessage(string message, IRCColor color)
        {
            SendChatMessageBackend(message, color);
        }

        public void SendChatMessageBackend(string message, IRCColor color)
        {
            AddMessage(new ChatMessage(ProgramConstants.PLAYERNAME, color.XnaColor, DateTime.Now, message));

            int spaceId = _spaceId != 0 ? _spaceId : _sessionManager.LobbySpaceId ?? 1;
            _ = _wsClient.SendMessageAsync(spaceId, message, IsChatChannel ? "room" : "lobby");
        }

        public void JoinBackend()
        {
            _ = _sessionManager.JoinSpaceAsync(_spaceId);
        }

        public void LeaveBackend()
        {
            _ = _sessionManager.LeaveSpaceAsync(_spaceId);
            ClearUsers();
        }

        public void UpdateFromSpace(SpaceResponse space)
        {
            _spaceId = space.Id;
            _channel = $"room:{space.Id}";
        }

        public void SetChannel(string channel)
        {
            _channel = channel;
        }

        public async Task LoadMembersAsync()
        {
            try
            {
                var members = await _apiClient.GetSpaceMembersAsync(_spaceId);
                var channelUsers = new List<ChannelUser>();

                foreach (var member in members)
                {
                    var ircUser = new IRCUser(member.Username)
                    {
                        IsGuest = false
                    };

                    var channelUser = new ChannelUser(ircUser)
                    {
                        IsAdmin = member.IsAdmin,
                        IsFriend = false
                    };

                    channelUsers.Add(channelUser);
                }

                OnUserListReceived(channelUsers);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load space members: {ex.Message}");
            }
        }

        private class NullConnection : Connection
        {
            public NullConnection(PlayerIdentityService playerIdentityService) : base(new NullConnectionManager(), new Random(), playerIdentityService)
            {
            }

            private class NullConnectionManager : IConnectionManager
            {
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

                public Channel? MainChannel { get; set; }
                public bool IsConnected => false;
                public bool IsAttemptingConnection => false;
                public List<IRCUser> UserList
                {
                    get => new List<IRCUser>();
                    set { }
                }

                public void OnWelcomeMessageReceived(string message) { }
                public void OnGenericServerMessageReceived(string message) { }
                public void OnAwayMessageReceived(string userName, string reason) { }
                public void OnChannelTopicReceived(string channelName, string topic) { }
                public void OnChannelTopicChanged(string userName, string channelName, string topic) { }
                public void OnUserListReceived(string channelName, string[] userList) { }
                public void OnWhoReplyReceived(string ident, string hostName, string userName, string extraInfo) { }
                public void OnChannelFull(string channelName) { }
                public void OnTargetChangeTooFast(string channelName, string message) { }
                public void OnChannelInviteOnly(string channelName) { }
                public void OnIncorrectChannelPassword(string channelName) { }
                public void OnCTCPParsed(string channelName, string userName, string message) { }
                public void OnNoticeMessageParsed(string notice, string userName) { }
                public void OnUserJoinedChannel(string channelName, string hostName, string userName, string ident) { }
                public void OnUserLeftChannel(string channelName, string userName) { }
                public void OnUserQuitIRC(string userName) { }
                public void OnChatMessageReceived(string receiver, string senderName, string senderIdent, string message) { }
                public void OnPrivateMessageReceived(string sender, string message) { }
                public void OnChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters) { }
                public void OnUserKicked(string channelName, string userName) { }
                public void OnErrorReceived(string errorMessage) { }
                public void OnNameAlreadyInUse() { }
                public void OnBannedFromChannel(string channelName) { }
                public void OnUserNicknameChange(string oldNickname, string newNickname) { }
                public void OnAttemptedServerChanged(string serverName) { }
                public void OnConnectAttemptFailed() { }
                public void OnConnectionLost(string reason) { }
                public void OnReconnectAttempt() { }
                public void OnDisconnected() { }
                public void OnConnected() { }
                public bool GetDisconnectStatus() => false;
                public void OnServerLatencyTested(int candidateCount, int closerCount) { }
                public void Connect() { }

                public bool IsCnCNetInitialized() => false;
                public Channel CreateChannel(string uiName, string channelName, bool persistent, bool isChatChannel, string password) => null;
                public void AddChannel(Channel channel) { }
                public void RemoveChannel(Channel channel) { }
                public IRCColor[] GetIRCColors() => Array.Empty<IRCColor>();
                public void LeaveFromChannel(Channel channel) { }
                public void SetMainChannel(Channel channel) { }
                public void SendCustomMessage(QueuedMessage qm) { }
                public void SendWhoIsMessage(string nick) { }
                public void RemoveChannelFromUser(string userName, string channelName) { }
                public Channel? FindChannel(string channelName) => null;
                public void Disconnect() { }
            }
        }
    }
}
