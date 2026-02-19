using ClientCore;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online.EventArguments;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;

namespace DTAClient.Online
{
    /// <summary>
    /// An interface for handling IRC messages.
    /// </summary>
    public interface IConnectionManager
    {
        event EventHandler<ServerMessageEventArgs> WelcomeMessageReceived;
        event EventHandler<UserAwayEventArgs> AwayMessageReceived;
        event EventHandler<WhoEventArgs> WhoReplyReceived;
        event EventHandler<CnCNetPrivateMessageEventArgs> PrivateMessageReceived;
        event EventHandler<PrivateCTCPEventArgs> PrivateCTCPReceived;
        event EventHandler<ChannelEventArgs> BannedFromChannel;

        event EventHandler<AttemptedServerEventArgs> AttemptedServerChanged;
        event EventHandler ConnectAttemptFailed;
        event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        event EventHandler ReconnectAttempt;
        event EventHandler Disconnected;
        event EventHandler Connected;

        event EventHandler<UserEventArgs> UserAdded;
        event EventHandler<UserEventArgs> UserGameIndexUpdated;
        event EventHandler<UserNameIndexEventArgs> UserRemoved;
        event EventHandler MultipleUsersAdded;

        Channel MainChannel { get; set; }

        bool IsConnected { get; }

        bool IsAttemptingConnection { get; }

        List<IRCUser> UserList { get; }

        bool IsCnCNetInitialized();

        Channel CreateChannel(string uiName, string channelName, bool persistent, bool isChatChannel, string password);

        void AddChannel(Channel channel);

        void RemoveChannel(Channel channel);

        IRCColor[] GetIRCColors();

        void LeaveFromChannel(Channel channel);

        void SetMainChannel(Channel channel);

        void SendCustomMessage(QueuedMessage qm);

        void SendWhoIsMessage(string nick);

        void RemoveChannelFromUser(string userName, string channelName);

        Channel? FindChannel(string channelName);

        void Disconnect();
        void OnWelcomeMessageReceived(string message);

        void OnGenericServerMessageReceived(string message);

        void OnAwayMessageReceived(string userName, string reason);

        void OnChannelTopicReceived(string channelName, string topic);

        void OnChannelTopicChanged(string userName, string channelName, string topic);

        void OnUserListReceived(string channelName, string[] userList);

        void OnWhoReplyReceived(string ident, string hostName, string userName, string extraInfo);

        void OnChannelFull(string channelName);

        void OnTargetChangeTooFast(string channelName, string message);

        void OnChannelInviteOnly(string channelName);

        void OnIncorrectChannelPassword(string channelName);

        void OnCTCPParsed(string channelName, string userName, string message);

        void OnNoticeMessageParsed(string notice, string userName);

        void OnUserJoinedChannel(string channelName, string hostName, string userName, string ident);

        void OnUserLeftChannel(string channelName, string userName);

        void OnUserQuitIRC(string userName);

        void OnChatMessageReceived(string receiver, string senderName, string senderIdent, string message);

        void OnPrivateMessageReceived(string sender, string message);

        void OnChannelModesChanged(string userName, string channelName, string modeString, List<string> modeParameters);

        void OnUserKicked(string channelName, string userName);

        void OnErrorReceived(string errorMessage);

        void OnNameAlreadyInUse();

        void OnBannedFromChannel(string channelName);

        void OnUserNicknameChange(string oldNickname, string newNickname);

        // **********************
        // Connection-related methods
        // **********************

        void OnAttemptedServerChanged(string serverName);

        void OnConnectAttemptFailed();

        void OnConnectionLost(string reason);

        void OnReconnectAttempt();

        void OnDisconnected();

        void OnConnected();

        bool GetDisconnectStatus();

        void OnServerLatencyTested(int candidateCount, int closerCount);

        void Connect();

        //public EventHandler<ServerMessageEventArgs> WelcomeMessageReceived;
        //public EventHandler<ServerMessageEventArgs> GenericServerMessageReceived;
        //public EventHandler<UserAwayEventArgs> AwayMessageReceived;
        //public EventHandler<ChannelTopicEventArgs> ChannelTopicReceived;
        //public EventHandler<UserListEventArgs> UserListReceived;
        //public EventHandler<WhoEventArgs> WhoReplyReceived;
        //public EventHandler<ChannelEventArgs> ChannelFull;
        //public EventHandler<ChannelEventArgs> IncorrectChannelPassword;

        //public event EventHandler<AttemptedServerEventArgs> AttemptedServerChanged;
        //public event EventHandler ConnectAttemptFailed;
        //public event EventHandler<ConnectionLostEventArgs> ConnectionLost;
        //public event EventHandler ReconnectAttempt;
    }
}
