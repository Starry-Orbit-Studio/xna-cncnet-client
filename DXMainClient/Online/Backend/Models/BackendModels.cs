#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DTAClient.Online.Backend.Models
{
    public class CreateGuestSessionRequest
    {
        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }
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
        public int? UserId { get; set; }

        [JsonPropertyName("is_guest")]
        public bool IsGuest { get; set; }

        [JsonPropertyName("guest_name")]
        public string? GuestName { get; set; }

        [JsonPropertyName("connected_at")]
        public DateTime ConnectedAt { get; set; }

        [JsonPropertyName("last_seen")]
        public DateTime LastSeen { get; set; }

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
        public int OwnerUserId { get; set; }

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

    public class WebSocketMessage
    {
        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public JsonElement? Data { get; set; }
    }
}
