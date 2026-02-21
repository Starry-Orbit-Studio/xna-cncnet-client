#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTAClient.Online.Backend.Models
{
    public class CreateGuestSessionRequest
    {
        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }
    }

    public class GuestLoginRequest
    {
        [JsonPropertyName("guest_uid")]
        public string GuestUid { get; set; } = string.Empty;

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; }

        [JsonPropertyName("hwid_list")]
        public List<string> HwidList { get; set; } = new();
    }

    public class AuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }

    public class ConnectTicketRequest
    {
        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }
    }

    public class ConnectTicketResponse
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("ws_ticket")]
        public string WsTicket { get; set; } = string.Empty;
    }

    public class BindUserRequest
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
    }

    public class SessionResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string? UserId { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }

        [JsonPropertyName("connected_at")]
        public DateTime ConnectedAt { get; set; }

        [JsonPropertyName("last_seen")]
        public DateTime? LastSeen { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }

    public class CreateSpaceRequest
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "lobby";

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("max_members")]
        public int MaxMembers { get; set; } = 100;

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }
    }

    public class UpdateSpaceRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("max_members")]
        public int? MaxMembers { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class SpaceResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("owner_user_id")]
        public int? OwnerUserId { get; set; }

        [JsonPropertyName("max_members")]
        public int MaxMembers { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class JoinSpaceRequest
    {
        [JsonPropertyName("space_id")]
        public int SpaceId { get; set; }
    }

    public class LeaveSpaceRequest
    {
        [JsonPropertyName("space_id")]
        public int SpaceId { get; set; }
    }

    public class SendMessageRequest
    {
        [JsonPropertyName("space_id")]
        public int SpaceId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "room";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatMessageResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("space_id")]
        public int SpaceId { get; set; }

        [JsonPropertyName("sender_session_id")]
        public string SenderSessionId { get; set; } = string.Empty;

        [JsonPropertyName("sender_user_id")]
        public int SenderUserId { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class SendFriendRequestRequest
    {
        [JsonPropertyName("friend_id")]
        public int FriendId { get; set; }
    }

    public class FriendshipResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("friend_id")]
        public int FriendId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class MuteUserRequest
    {
        [JsonPropertyName("target_user_id")]
        public int TargetUserId { get; set; }

        [JsonPropertyName("space_id")]
        public int? SpaceId { get; set; }

        [JsonPropertyName("duration_minutes")]
        public int DurationMinutes { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    public class MuteResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("target_user_id")]
        public int TargetUserId { get; set; }

        [JsonPropertyName("space_id")]
        public int? SpaceId { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime ExpiresAt { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
    }

    public class MuteCheckResponse
    {
        [JsonPropertyName("is_muted")]
        public bool IsMuted { get; set; }
    }

    public class SpaceMemberResponse
    {
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("is_admin")]
        public bool IsAdmin { get; set; }

        [JsonPropertyName("is_online")]
        public bool IsOnline { get; set; }

        [JsonPropertyName("joined_at")]
        public DateTime JoinedAt { get; set; }
    }

    public class OnlineUserResponse
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("last_seen")]
        public DateTime? LastSeen { get; set; }
    }

    public class OnlineUsersResponse
    {
        [JsonPropertyName("users")]
        public List<OnlineUserResponse> Users { get; set; } = new();

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("guest_count")]
        public int GuestCount { get; set; }

        [JsonPropertyName("user_count")]
        public int UserCount { get; set; }
    }

    public class WebSocketMessage
    {
        [JsonPropertyName("event_type")]
        public string EventType { get; set; } = string.Empty;

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }
    }

    public class WebSocketClientMessage
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = string.Empty;

        [JsonPropertyName("channel")]
        public string? Channel { get; set; }

        [JsonPropertyName("channel_type")]
        public string? ChannelType { get; set; }

        [JsonPropertyName("channel_id")]
        public string? ChannelId { get; set; }

        [JsonPropertyName("space_id")]
        public int? SpaceId { get; set; }

        [JsonPropertyName("payload")]
        public MessagePayload? Payload { get; set; }
    }

    public class MessagePayload
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class UserJoinedEventData
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }
    }

    public class UserLeftEventData
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    public class UserStatusChangedEventData
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("old_status")]
        public string OldStatus { get; set; } = string.Empty;

        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; } = string.Empty;
    }

    public class RoomCreatedEventData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("owner_user_id")]
        public int? OwnerUserId { get; set; }

        [JsonPropertyName("max_members")]
        public int MaxMembers { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class RoomUpdatedEventData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("owner_user_id")]
        public int? OwnerUserId { get; set; }

        [JsonPropertyName("max_members")]
        public int MaxMembers { get; set; }

        [JsonPropertyName("is_private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("member_count")]
        public int MemberCount { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class RoomDeletedEventData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    public class RoomMemberJoinedEventData
    {
        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;
    }

    public class RoomMemberLeftEventData
    {
        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    public class RoomStatusChangedEventData
    {
        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("old_status")]
        public string OldStatus { get; set; } = string.Empty;

        [JsonPropertyName("new_status")]
        public string NewStatus { get; set; } = string.Empty;
    }

    public class MessageSentEventData
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("room_id")]
        public string RoomId { get; set; } = string.Empty;

        [JsonPropertyName("sender_session_id")]
        public string SenderSessionId { get; set; } = string.Empty;

        [JsonPropertyName("sender_user_id")]
        public string SenderUserId { get; set; } = string.Empty;

        [JsonPropertyName("sender_nickname")]
        public string SenderNickname { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class MessageEditedEventData
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;

        [JsonPropertyName("new_content")]
        public string NewContent { get; set; } = string.Empty;
    }

    public class MessageDeletedEventData
    {
        [JsonPropertyName("message_id")]
        public string MessageId { get; set; } = string.Empty;
    }

    public class AnnouncementEventData
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;
    }

    public class NotificationEventData
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }

    public class MatchFoundEventData
    {
        [JsonPropertyName("match_id")]
        public string MatchId { get; set; } = string.Empty;

        [JsonPropertyName("room_id")]
        public int RoomId { get; set; }

        [JsonPropertyName("participants")]
        public List<MatchParticipant> Participants { get; set; } = new();
    }

    public class MatchParticipant
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;
    }

    public class MatchCancelledEventData
    {
        [JsonPropertyName("match_id")]
        public string MatchId { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }

    public class ReadyEventData
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_info")]
        public ReadyUserInfo UserInfo { get; set; } = new();

        [JsonPropertyName("lobby_info")]
        public ReadyLobbyInfo LobbyInfo { get; set; } = new();

        [JsonPropertyName("subscriptions")]
        public List<string> Subscriptions { get; set; } = new();
    }

    public class ReadyUserInfo
    {
        [JsonPropertyName("session_id")]
        public string SessionId { get; set; } = string.Empty;

        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;

        [JsonPropertyName("avatar")]
        public string? Avatar { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("role")]
        public string? Role { get; set; }
    }

    public class ReadyLobbyInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = string.Empty;
    }

    public class ErrorEventData
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
