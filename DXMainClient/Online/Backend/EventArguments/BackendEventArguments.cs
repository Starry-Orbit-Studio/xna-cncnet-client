#nullable enable
using System;
using DTAClient.Online.Backend.Models;

namespace DTAClient.Online.Backend.EventArguments
{
    public class SessionEventArgs : EventArgs
    {
        public SessionResponse Session { get; }

        public SessionEventArgs(SessionResponse session)
        {
            Session = session;
        }
    }

    public class SpaceEventArgs : EventArgs
    {
        public SpaceResponse? Space { get; }
        public BackendChannel Channel { get; }

        public SpaceEventArgs(SpaceResponse? space, BackendChannel channel)
        {
            Space = space;
            Channel = channel;
        }
    }

    public class WebSocketMessageEventArgs : EventArgs
    {
        public WebSocketMessage Message { get; }

        public WebSocketMessageEventArgs(WebSocketMessage message)
        {
            Message = message;
        }
    }

    public class WebSocketErrorEventArgs : EventArgs
    {
        public string Message { get; }

        public WebSocketErrorEventArgs(string message)
        {
            Message = message;
        }
    }

    public class UserJoinedEventArgs : EventArgs
    {
        public UserJoinedEventData Data { get; }

        public UserJoinedEventArgs(UserJoinedEventData data)
        {
            Data = data;
        }
    }

    public class UserLeftEventArgs : EventArgs
    {
        public UserLeftEventData Data { get; }

        public UserLeftEventArgs(UserLeftEventData data)
        {
            Data = data;
        }
    }

    public class UserStatusChangedEventArgs : EventArgs
    {
        public UserStatusChangedEventData Data { get; }

        public UserStatusChangedEventArgs(UserStatusChangedEventData data)
        {
            Data = data;
        }
    }

    public class RoomCreatedEventArgs : EventArgs
    {
        public RoomCreatedEventData Data { get; }

        public RoomCreatedEventArgs(RoomCreatedEventData data)
        {
            Data = data;
        }
    }

    public class RoomUpdatedEventArgs : EventArgs
    {
        public RoomUpdatedEventData Data { get; }

        public RoomUpdatedEventArgs(RoomUpdatedEventData data)
        {
            Data = data;
        }
    }

    public class RoomDeletedEventArgs : EventArgs
    {
        public RoomDeletedEventData Data { get; }

        public RoomDeletedEventArgs(RoomDeletedEventData data)
        {
            Data = data;
        }
    }

    public class RoomMemberJoinedEventArgs : EventArgs
    {
        public RoomMemberJoinedEventData Data { get; }

        public RoomMemberJoinedEventArgs(RoomMemberJoinedEventData data)
        {
        Data = data;
        }
    }

    public class RoomMemberLeftEventArgs : EventArgs
    {
        public RoomMemberLeftEventData Data { get; }

        public RoomMemberLeftEventArgs(RoomMemberLeftEventData data)
        {
            Data = data;
        }
    }

    public class RoomStatusChangedEventArgs : EventArgs
    {
        public RoomStatusChangedEventData Data { get; }

        public RoomStatusChangedEventArgs(RoomStatusChangedEventData data)
        {
            Data = data;
        }
    }

    public class MessageSentEventArgs : EventArgs
    {
        public MessageSentEventData Data { get; }

        public MessageSentEventArgs(MessageSentEventData data)
        {
            Data = data;
        }
    }

    public class MessageEditedEventArgs : EventArgs
    {
        public MessageEditedEventData Data { get; }

        public MessageEditedEventArgs(MessageEditedEventData data)
        {
            Data = data;
        }
    }

    public class MessageDeletedEventArgs : EventArgs
    {
        public MessageDeletedEventData Data { get; }

        public MessageDeletedEventArgs(MessageDeletedEventData data)
        {
            Data = data;
        }
    }

    public class AnnouncementEventArgs : EventArgs
    {
        public AnnouncementEventData Data { get; }

        public AnnouncementEventArgs(AnnouncementEventData data)
        {
            Data = data;
        }
    }

    public class NotificationEventArgs : EventArgs
    {
        public NotificationEventData Data { get; }

        public NotificationEventArgs(NotificationEventData data)
        {
            Data = data;
        }
    }

    public class MatchFoundEventArgs : EventArgs
    {
        public MatchFoundEventData Data { get; }

        public MatchFoundEventArgs(MatchFoundEventData data)
        {
            Data = data;
        }
    }

    public class MatchCancelledEventArgs : EventArgs
    {
        public MatchCancelledEventData Data { get; }

        public MatchCancelledEventArgs(MatchCancelledEventData data)
        {
            Data = data;
        }
    }

    public class OnlineUsersEventArgs : EventArgs
    {
        public OnlineUsersResponse Data { get; }

        public OnlineUsersEventArgs(OnlineUsersResponse data)
        {
            Data = data;
        }
    }

    public class ReadyEventArgs : EventArgs
    {
        public ReadyEventData Data { get; }

        public ReadyEventArgs(ReadyEventData data)
        {
            Data = data;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventData Data { get; }

        public ErrorEventArgs(ErrorEventData data)
        {
            Data = data;
        }
    }
}