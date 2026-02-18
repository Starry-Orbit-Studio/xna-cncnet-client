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

namespace DTAClient.Online.Backend
{
    public class BackendChannel : Channel
    {
        private readonly BackendApiClient _apiClient;
        private readonly BackendSessionManager _sessionManager;
        private int _spaceId;

        public BackendChannel(
            string uiName,
            string channelName,
            bool persistent,
            bool isChatChannel,
            string? password,
            BackendApiClient apiClient,
            BackendSessionManager sessionManager)
            : base(uiName, channelName, persistent, isChatChannel, password ?? string.Empty, new NullConnection())
        {
            _apiClient = apiClient;
            _sessionManager = sessionManager;
        }

        public void SendChatMessageBackend(string message, IRCColor color)
        {
            AddMessage(new ChatMessage(ProgramConstants.PLAYERNAME, color.XnaColor, DateTime.Now, message));

            _ = _apiClient.SendMessageAsync(new SendMessageRequest
            {
                SpaceId = _spaceId,
                Type = IsChatChannel ? "room" : "lobby",
                Content = message
            });
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
            public NullConnection() : base(new NullConnectionManager(), new Random())
            {
            }

            private class NullConnectionManager : IConnectionManager
            {
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
            }
        }
    }
}
