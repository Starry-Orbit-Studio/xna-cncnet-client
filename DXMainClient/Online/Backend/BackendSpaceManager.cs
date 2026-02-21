#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DTAClient.Online.Backend.EventArguments;
using DTAClient.Online.Backend.Models;

namespace DTAClient.Online.Backend
{
    public class BackendSpaceManager
    {
        private readonly BackendApiClient _apiClient;
        private readonly BackendSessionManager _sessionManager;
        private readonly BackendWebSocketClient _wsClient;
        private readonly PlayerIdentityService _playerIdentityService;
        private readonly Dictionary<int, BackendChannel> _channels = new();

        public event EventHandler<SpaceEventArgs>? SpaceCreated;
        public event EventHandler<SpaceEventArgs>? SpaceUpdated;
        public event EventHandler<SpaceEventArgs>? SpaceDeleted;

        public BackendSpaceManager(BackendApiClient apiClient, BackendSessionManager sessionManager, BackendWebSocketClient wsClient, PlayerIdentityService playerIdentityService)
        {
            _apiClient = apiClient;
            _sessionManager = sessionManager;
            _wsClient = wsClient;
            _playerIdentityService = playerIdentityService;
        }

        public async Task<BackendChannel> CreateLobbyAsync(string name, int maxMembers = 100, bool isPrivate = false)
        {
            var request = new CreateSpaceRequest
            {
                Type = "lobby",
                Name = name,
                MaxMembers = maxMembers,
                IsPrivate = isPrivate
            };

            var space = await _apiClient.CreateSpaceAsync(request);
            var channel = CreateChannelFromSpace(space);

            _channels[space.Id] = channel;
            SpaceCreated?.Invoke(this, new SpaceEventArgs(space, channel));

            return channel;
        }

        public async Task<BackendChannel> CreateRoomAsync(string name, int maxMembers, bool isPrivate)
        {
            var request = new CreateSpaceRequest
            {
                Type = "room",
                Name = name,
                MaxMembers = maxMembers,
                IsPrivate = isPrivate
            };

            var space = await _apiClient.CreateSpaceAsync(request);
            var channel = CreateChannelFromSpace(space);

            _channels[space.Id] = channel;
            SpaceCreated?.Invoke(this, new SpaceEventArgs(space, channel));

            return channel;
        }

        public async Task<List<BackendChannel>> GetLobbiesAsync()
        {
            var spaces = await _apiClient.GetSpacesAsync("lobby");
            return spaces.Select(CreateChannelFromSpace).ToList();
        }

        public async Task<List<BackendChannel>> GetRoomsAsync()
        {
            var spaces = await _apiClient.GetSpacesAsync("room");
            return spaces.Select(CreateChannelFromSpace).ToList();
        }

        public async Task<List<SpaceResponse>> GetRoomSpacesAsync()
        {
            return await _apiClient.GetSpacesAsync("room");
        }

        public async Task<List<SpaceMemberResponse>> GetSpaceMembersAsync(int spaceId)
        {
            return await _apiClient.GetSpaceMembersAsync(spaceId);
        }

        public async Task<BackendChannel?> GetChannelAsync(int spaceId)
        {
            if (_channels.TryGetValue(spaceId, out var channel))
                return channel;

            var space = await _apiClient.GetSpaceAsync(spaceId);
            channel = CreateChannelFromSpace(space);
            _channels[spaceId] = channel;

            return channel;
        }

        public async Task JoinChannelAsync(int spaceId)
        {
            await _apiClient.JoinSpaceAsync(spaceId);

            var channel = await GetChannelAsync(spaceId);
            if (channel != null)
            {
                await channel.LoadMembersAsync();
            }
        }

        public async Task LeaveChannelAsync(int spaceId)
        {
            await _apiClient.LeaveSpaceAsync(spaceId);

            if (_channels.TryGetValue(spaceId, out var channel))
            {
                _channels.Remove(spaceId);
            }
        }

        public async Task UpdateChannelAsync(int spaceId, UpdateSpaceRequest request)
        {
            var space = await _apiClient.UpdateSpaceAsync(spaceId, request);

            if (_channels.TryGetValue(spaceId, out var channel))
            {
                channel.UpdateFromSpace(space);
                SpaceUpdated?.Invoke(this, new SpaceEventArgs(space, channel));
            }
        }

        public async Task DeleteChannelAsync(int spaceId)
        {
            await _apiClient.DeleteSpaceAsync(spaceId);

            if (_channels.TryGetValue(spaceId, out var channel))
            {
                _channels.Remove(spaceId);
                SpaceDeleted?.Invoke(this, new SpaceEventArgs(null, channel));
            }
        }

        private BackendChannel CreateChannelFromSpace(SpaceResponse space)
        {
            var channel = new BackendChannel(
                space.Name,
                $"#{space.Id}",
                false,
                space.Type == "lobby" || space.Type == "room",
                null,
                _apiClient,
                _sessionManager,
                _wsClient,
                _playerIdentityService
            );
            channel.UpdateFromSpace(space);
            return channel;
        }
    }
}
