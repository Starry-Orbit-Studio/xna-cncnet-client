#nullable enable
using System;
using System.Collections.Generic;
using DTAClient.Online.RedAlert;
using DTAClient.Online.DomainAction.Payloads;
using DTAClient.Online.Backend.Models;

namespace DTAClient.Online.SharedModels
{
    /// <summary>
    /// 模型映射器，用于在不同模型体系之间进行转换
    /// </summary>
    public class ModelMapper
    {
        public ModelMapper()
        {
        }

        #region 用户相关映射

        /// <summary>
        /// 从 REST API 用户模型转换为统一模型
        /// </summary>
        public UnifiedUserFullCard MapFromRestApi(DTAClient.Online.RedAlert.UserFullCard restUser)
        {
            if (restUser == null) throw new ArgumentNullException(nameof(restUser));

            return new UnifiedUserFullCard
            {
                Id = restUser.Id,
                UserId = restUser.Id,
                Username = restUser.Username,
                DisplayName = restUser.DisplayName,
                AvatarUrl = restUser.AvatarUrl,
                IsGuest = restUser.IsGuest,
                IsOnline = restUser.IsOnline,
                LastSeen = restUser.LastSeen,
                GameStats = restUser.GameStats != null ? MapFromRestApi(restUser.GameStats) : null,
                CreatedAt = restUser.CreatedAt
            };
        }

        /// <summary>
        /// 从 Domain-Action 协议用户模型转换为统一模型
        /// </summary>
        public UnifiedUserFullCard MapFromDomainAction(DTAClient.Online.DomainAction.Payloads.UserFullCard domainUser)
        {
            if (domainUser == null) throw new ArgumentNullException(nameof(domainUser));

            return new UnifiedUserFullCard
            {
                Id = domainUser.UserId,
                UserId = domainUser.UserId,
                Nickname = domainUser.Nickname,
                Avatar = domainUser.Avatar,
                IsGuest = domainUser.IsGuest,
                IsOnline = domainUser.IsOnline,
                LastSeenTimestamp = domainUser.LastSeen,
                Level = domainUser.Level,
                ClanTag = domainUser.ClanTag,
                Status = domainUser.Status
            };
        }

        /// <summary>
        /// 从统一模型转换为 REST API 用户模型
        /// </summary>
        public DTAClient.Online.RedAlert.UserFullCard MapToRestApi(UnifiedUserFullCard unifiedUser)
        {
            if (unifiedUser == null) throw new ArgumentNullException(nameof(unifiedUser));

            return new DTAClient.Online.RedAlert.UserFullCard
            {
                Id = unifiedUser.Id,
                Username = unifiedUser.Username ?? unifiedUser.Nickname ?? unifiedUser.Id,
                DisplayName = unifiedUser.DisplayName ?? unifiedUser.Username ?? unifiedUser.Nickname,
                AvatarUrl = unifiedUser.GetAvatarUrl(),
                IsGuest = unifiedUser.IsGuest,
                IsOnline = unifiedUser.IsOnline,
                LastSeen = unifiedUser.GetLastSeen(),
                GameStats = unifiedUser.GameStats != null ? MapToRestApi(unifiedUser.GameStats) : new UserGameStats(),
                CreatedAt = unifiedUser.GetCreatedAt() ?? DateTime.UtcNow
            };
        }

        /// <summary>
        /// 从统一模型转换为 Domain-Action 协议用户模型
        /// </summary>
        public DTAClient.Online.DomainAction.Payloads.UserFullCard MapToDomainAction(UnifiedUserFullCard unifiedUser)
        {
            if (unifiedUser == null) throw new ArgumentNullException(nameof(unifiedUser));

            return new DTAClient.Online.DomainAction.Payloads.UserFullCard
            {
                UserId = unifiedUser.UserId,
                Nickname = unifiedUser.Nickname ?? unifiedUser.Username ?? unifiedUser.DisplayName ?? unifiedUser.Id,
                Avatar = unifiedUser.GetAvatarUrl(),
                Level = unifiedUser.Level ?? 0,
                ClanTag = unifiedUser.ClanTag,
                IsOnline = unifiedUser.IsOnline,
                IsGuest = unifiedUser.IsGuest,
                LastSeen = unifiedUser.LastSeenTimestamp ?? 
                          (unifiedUser.LastSeen.HasValue ? 
                           (long?)new DateTimeOffset(unifiedUser.LastSeen.Value).ToUnixTimeSeconds() : null),
                Status = unifiedUser.Status
            };
        }

        private UnifiedUserGameStats MapFromRestApi(UserGameStats restStats)
        {
            if (restStats == null) throw new ArgumentNullException(nameof(restStats));

            return new UnifiedUserGameStats
            {
                TotalGames = restStats.TotalGames,
                Wins = restStats.Wins,
                Losses = restStats.Losses,
                Rating = restStats.Rating,
                Rank = restStats.Rank
            };
        }

        private UserGameStats MapToRestApi(UnifiedUserGameStats unifiedStats)
        {
            if (unifiedStats == null) throw new ArgumentNullException(nameof(unifiedStats));

            return new UserGameStats
            {
                TotalGames = unifiedStats.TotalGames,
                Wins = unifiedStats.Wins,
                Losses = unifiedStats.Losses,
                Rating = unifiedStats.Rating ?? 0,
                Rank = unifiedStats.Rank
            };
        }

        #endregion

        #region 频道相关映射

        /// <summary>
        /// 从 REST API 频道模型转换为统一模型
        /// </summary>
        public UnifiedChannelInfo MapFromRestApi(ChannelResponse restChannel)
        {
            if (restChannel == null) throw new ArgumentNullException(nameof(restChannel));

            return new UnifiedChannelInfo
            {
                ChannelIdString = restChannel.Id,
                Name = restChannel.Name,
                Description = restChannel.Description,
                Type = restChannel.Type,
                MemberCount = restChannel.MemberCount,
                OnlineCount = restChannel.MemberCount, // REST API 使用 member_count
                RoomCount = restChannel.RoomCount,
                MaxCapacity = 100, // 默认值
                IsDefault = false, // REST API 没有此字段
                RequiresPassword = false // REST API 没有此字段
            };
        }

        /// <summary>
        /// 从 Domain-Action 协议频道模型转换为统一模型
        /// </summary>
        public UnifiedChannelInfo MapFromDomainAction(ChannelInfo domainChannel)
        {
            if (domainChannel == null) throw new ArgumentNullException(nameof(domainChannel));

            return new UnifiedChannelInfo
            {
                Id = domainChannel.Id,
                Name = domainChannel.Name,
                Description = domainChannel.Description,
                OnlineCount = domainChannel.OnlineCount,
                MaxCapacity = domainChannel.MaxCapacity,
                IsDefault = domainChannel.IsDefault,
                RequiresPassword = domainChannel.RequiresPassword,
                RegionTag = domainChannel.RegionTag,
                LanguageTag = domainChannel.LanguageTag
            };
        }

        #endregion

        #region 房间相关映射

        /// <summary>
        /// 从 REST API 房间同步事件转换为统一模型
        /// </summary>
        public UnifiedRoomSyncEvent MapFromRestApi(RoomSyncEvent restRoom)
        {
            if (restRoom == null) throw new ArgumentNullException(nameof(restRoom));

            return new UnifiedRoomSyncEvent
            {
                RoomId = restRoom.RoomId,
                ChannelId = restRoom.ChannelId,
                HostId = restRoom.HostId,
                Settings = MapFromRestApi(restRoom.Settings),
                Members = restRoom.Members.ConvertAll(MapFromRestApi),
                CreatedAt = restRoom.CreatedAt,
                UpdatedAt = restRoom.UpdatedAt,
                State = restRoom.State
            };
        }

        /// <summary>
        /// 从 Domain-Action 协议房间同步载荷转换为统一模型
        /// </summary>
        public UnifiedRoomSyncEvent MapFromDomainAction(RoomSyncPayload domainRoom)
        {
            if (domainRoom == null) throw new ArgumentNullException(nameof(domainRoom));

            return new UnifiedRoomSyncEvent
            {
                RoomId = domainRoom.RoomId,
                Settings = MapFromDomainAction(domainRoom.Settings),
                Members = domainRoom.Members.ConvertAll(MapFromDomainAction),
                SyncReason = domainRoom.SyncReason,
                TriggeredBy = domainRoom.TriggeredBy,
                SyncTimestamp = domainRoom.SyncTimestamp
            };
        }

        /// <summary>
        /// 从 REST API 房间设置转换为统一模型
        /// </summary>
        public UnifiedRoomSettings MapFromRestApi(DTAClient.Online.RedAlert.RoomSettings restSettings)
        {
            if (restSettings == null) throw new ArgumentNullException(nameof(restSettings));

            return new UnifiedRoomSettings
            {
                Name = restSettings.Name,
                Password = restSettings.Password,
                MaxPlayers = restSettings.MaxPlayers,
                GameMode = restSettings.GameMode,
                MapName = restSettings.MapName,
                MapHash = restSettings.MapHash,
                GameVersion = restSettings.GameVersion,
                IsPrivate = restSettings.IsPrivate,
                AllowSpectators = restSettings.AllowSpectators
            };
        }

        /// <summary>
        /// 从 Domain-Action 协议房间设置转换为统一模型
        /// </summary>
        public UnifiedRoomSettings MapFromDomainAction(DTAClient.Online.DomainAction.Payloads.RoomSettings domainSettings)
        {
            if (domainSettings == null) throw new ArgumentNullException(nameof(domainSettings));

            return new UnifiedRoomSettings
            {
                Name = domainSettings.Name,
                Password = null, // Domain-Action 协议没有此字段
                MaxPlayers = domainSettings.MaxPlayers,
                MaxMembers = domainSettings.MaxPlayers, // 使用 MaxPlayers
                GameMode = domainSettings.GameMode,
                MapName = domainSettings.MapName,
                MapHash = domainSettings.MapHash,
                GameVersion = domainSettings.GameType, // 使用 GameType 代替 GameVersion
                IsPrivate = domainSettings.IsPrivate,
                AllowSpectators = true // Domain-Action 协议没有此字段，默认允许
            };
        }

        /// <summary>
        /// 从 REST API 房间成员转换为统一模型
        /// </summary>
        public UnifiedRoomMember MapFromRestApi(RoomMemberInfo restMember)
        {
            if (restMember == null) throw new ArgumentNullException(nameof(restMember));

            return new UnifiedRoomMember
            {
                UserId = restMember.UserId,
                Username = restMember.Username,
                IsReady = restMember.IsReady,
                IsHost = restMember.IsHost,
                SlotIndex = restMember.SlotIndex,
                Team = restMember.Team,
                Color = restMember.Color,
                JoinedAt = restMember.JoinedAt,
                Ping = restMember.Ping
            };
        }

        /// <summary>
        /// 从 Domain-Action 协议房间成员转换为统一模型
        /// </summary>
        public UnifiedRoomMember MapFromDomainAction(RoomMember domainMember)
        {
            if (domainMember == null) throw new ArgumentNullException(nameof(domainMember));

            return new UnifiedRoomMember
            {
                UserId = domainMember.UserId,
                SessionId = domainMember.SessionId,
                Nickname = domainMember.Nickname,
                IsReady = domainMember.IsReady,
                IsOwner = domainMember.IsOwner,
                Avatar = domainMember.Avatar,
                Level = domainMember.Level,
                ClanTag = domainMember.ClanTag,
                IsGuest = domainMember.IsGuest,
                ConnectionStatus = domainMember.ConnectionStatus,
                JoinedAtTimestamp = domainMember.JoinedAt
            };
        }

        #endregion

        #region 认证相关映射

        /// <summary>
        /// 从 REST API 认证令牌响应转换为统一模型
        /// </summary>
        public UnifiedAuthTokenResponse MapFromRestApi(DTAClient.Online.RedAlert.AuthTokenResponse restAuth)
        {
            if (restAuth == null) throw new ArgumentNullException(nameof(restAuth));

            return new UnifiedAuthTokenResponse
            {
                AccessToken = restAuth.AccessToken,
                TokenType = restAuth.TokenType
            };
        }

        /// <summary>
        /// 从 REST API 连接票据响应转换为统一模型
        /// </summary>
        public UnifiedConnectTicketResponse MapFromRestApi(DTAClient.Online.RedAlert.ConnectTicketResponse restTicket)
        {
            if (restTicket == null) throw new ArgumentNullException(nameof(restTicket));

            return new UnifiedConnectTicketResponse
            {
                SessionId = restTicket.SessionId,
                WsTicket = restTicket.WsTicket
            };
        }

        /// <summary>
        /// 从旧后端模型连接票据响应转换为统一模型
        /// </summary>
        public UnifiedConnectTicketResponse MapFromBackend(DTAClient.Online.Backend.Models.ConnectTicketResponse backendTicket)
        {
            if (backendTicket == null) throw new ArgumentNullException(nameof(backendTicket));

            return new UnifiedConnectTicketResponse
            {
                SessionId = backendTicket.SessionId,
                WsTicket = backendTicket.WsTicket
            };
        }

        #endregion

        #region 批量映射

        /// <summary>
        /// 批量转换 REST API 频道列表
        /// </summary>
        public List<UnifiedChannelInfo> MapFromRestApi(List<ChannelResponse> restChannels)
        {
            return restChannels.ConvertAll(MapFromRestApi);
        }

        /// <summary>
        /// 批量转换 Domain-Action 协议频道列表
        /// </summary>
        public List<UnifiedChannelInfo> MapFromDomainAction(List<ChannelInfo> domainChannels)
        {
            return domainChannels.ConvertAll(MapFromDomainAction);
        }

        /// <summary>
        /// 批量转换 REST API 房间成员列表
        /// </summary>
        public List<UnifiedRoomMember> MapFromRestApi(List<RoomMemberInfo> restMembers)
        {
            return restMembers.ConvertAll(MapFromRestApi);
        }

        /// <summary>
        /// 批量转换 Domain-Action 协议房间成员列表
        /// </summary>
        public List<UnifiedRoomMember> MapFromDomainAction(List<RoomMember> domainMembers)
        {
            return domainMembers.ConvertAll(MapFromDomainAction);
        }

        #endregion

        // 日志辅助方法（已移除 ILogger 依赖）
    }
}