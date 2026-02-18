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
}
