#nullable enable
using System;
using System.Threading.Tasks;
using ClientCore;
using Rampastring.Tools;
using DTAClient.Online.DomainAction;
using DTAClient.Online.DomainAction.Payloads;

namespace DTAClient.Online.DomainAction.Examples
{
    /// <summary>
    /// Domain-Action 协议使用示例
    /// </summary>
    public static class DomainActionProtocolExample
    {
        /// <summary>
        /// 演示基本连接和消息处理
        /// </summary>
        public static async Task RunBasicExample()
        {
            Logger.Log("[Example] Starting Domain-Action protocol example");
            
            // 1. 创建客户端
            string backendUrl = "https://backend.example.com";
            var client = new DomainActionWebSocketClient(backendUrl);
            
            // 2. 创建分发器和处理器
            var dispatcher = new DomainActionDispatcher();
            var systemHandler = new SystemHandler();
            var roomHandler = new RoomHandler();
            var channelHandler = new ChannelHandler();
            
            // 3. 注册处理器
            dispatcher.RegisterHandler(systemHandler);
            dispatcher.RegisterHandler(roomHandler);
            dispatcher.RegisterHandler(channelHandler);
            
            // 4. 设置客户端的分发器
            client.Dispatcher = dispatcher;
            
            // 5. 订阅事件
            systemHandler.Connected += (sender, payload) =>
            {
                Logger.Log($"[Example] Connected with session: {payload.SessionId}");
                
                // 连接成功后加入默认频道
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    
                    // 示例：发送加入频道请求
                    var joinChannelMessage = new DomainActionMessage
                    {
                        Domain = Domains.CHANNEL,
                        Action = Actions.JOIN_CHANNEL,
                        TargetId = "global",
                        Payload = System.Text.Json.JsonSerializer.SerializeToElement(new
                        {
                            channel_id = "global"
                        })
                    };
                    
                    await client.SendAsync(joinChannelMessage);
                });
            };
            
            systemHandler.ErrorReceived += (sender, payload) =>
            {
                Logger.Log($"[Example] Error: {payload.Code} - {payload.Reason}");
            };
            
            roomHandler.RoomCreated += (sender, payload) =>
            {
                Logger.Log($"[Example] Room created: {payload.RoomId}");
            };
            
            channelHandler.ChannelJoined += (sender, payload) =>
            {
                Logger.Log($"[Example] Joined channel: {payload.ChannelId}, online count: {payload.OnlineCount}");
            };
            
            channelHandler.NewChatReceived += (sender, payload) =>
            {
                Logger.Log($"[Example] New chat from {payload.Sender.UserId}: {payload.Content}");
            };
            
            // 6. 连接（需要有效的ticket）
            try
            {
                // 注意：实际使用中需要从认证接口获取ticket
                string ticket = "demo_ticket_123";
                await client.ConnectAsync(ticket);
                
                // 保持连接一段时间以演示
                await Task.Delay(5000);
                
                // 示例：创建房间
                var createRoomMessage = new DomainActionMessage
                {
                    Domain = Domains.ROOM,
                    Action = Actions.CREATE_ROOM,
                    Payload = System.Text.Json.JsonSerializer.SerializeToElement(new CreateRoomRequest
                    {
                        Name = "Example Room",
                        MapName = "Example Map",
                        MapHash = "md5_hash_here",
                        MaxPlayers = 4,
                        IsPrivate = false
                    })
                };
                
                // 发送并等待响应
                var response = await client.SendWithResponseAsync<RoomSyncPayload>(
                    createRoomMessage, 
                    TimeSpan.FromSeconds(10)
                );
                
                if (response != null)
                {
                    Logger.Log($"[Example] Room created successfully: {response.RoomId}");
                }
                
                // 保持连接一段时间
                await Task.Delay(10000);
                
                // 断开连接
                await client.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Logger.Log($"[Example] Error: {ex.Message}");
            }
            
            Logger.Log("[Example] Example completed");
        }
        
        /// <summary>
        /// 演示房间生命周期
        /// </summary>
        public static async Task RunRoomLifecycleExample(IDomainActionClient client)
        {
            Logger.Log("[Example] Starting room lifecycle example");
            
            // 假设已连接并已设置处理器
            
            // 1. 创建房间
            var createRoomRequest = new CreateRoomRequest
            {
                Name = "Test Room",
                MapName = "Test Map",
                MapHash = "test_hash",
                MaxPlayers = 4,
                IsPrivate = false
            };
            
            var createRoomMessage = new DomainActionMessage
            {
                Domain = Domains.ROOM,
                Action = Actions.CREATE_ROOM,
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(createRoomRequest)
            };
            
            var roomResponse = await client.SendWithResponseAsync<RoomSyncPayload>(createRoomMessage);
            if (roomResponse == null)
            {
                Logger.Log("[Example] Failed to create room");
                return;
            }
            
            string roomId = roomResponse.RoomId;
            Logger.Log($"[Example] Room created: {roomId}");
            
            // 2. 设置准备状态
            var setReadyMessage = new DomainActionMessage
            {
                Domain = Domains.ROOM,
                Action = Actions.SET_READY,
                TargetId = roomId,
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    is_ready = true
                })
            };
            
            await client.SendAsync(setReadyMessage);
            Logger.Log("[Example] Set ready status");
            
            // 3. 开始游戏（需要房间内所有玩家都准备）
            var startGameMessage = new DomainActionMessage
            {
                Domain = Domains.ROOM,
                Action = Actions.START_GAME,
                TargetId = roomId
            };
            
            await client.SendAsync(startGameMessage);
            Logger.Log("[Example] Start game requested");
            
            // 4. 等待 GAME_STARTING 消息
            // （在实际应用中，通过事件处理器处理）
            
            Logger.Log("[Example] Room lifecycle example completed");
        }
        
        /// <summary>
        /// 演示频道聊天
        /// </summary>
        public static async Task RunChannelChatExample(IDomainActionClient client, string channelId)
        {
            Logger.Log("[Example] Starting channel chat example");
            
            // 1. 加入频道
            var joinChannelMessage = new DomainActionMessage
            {
                Domain = Domains.CHANNEL,
                Action = Actions.JOIN_CHANNEL,
                TargetId = channelId,
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    channel_id = channelId
                })
            };
            
            await client.SendAsync(joinChannelMessage);
            Logger.Log($"[Example] Joined channel: {channelId}");
            
            // 2. 发送聊天消息
            var sendChatMessage = new DomainActionMessage
            {
                Domain = Domains.CHANNEL,
                Action = Actions.SEND_CHAT,
                TargetId = channelId,
                Payload = System.Text.Json.JsonSerializer.SerializeToElement(new
                {
                    message = "Hello everyone from Domain-Action client!",
                    channel_id = channelId
                })
            };
            
            await client.SendAsync(sendChatMessage);
            Logger.Log("[Example] Chat message sent");
            
            // 3. 接收聊天消息通过 NewChatReceived 事件处理
            
            Logger.Log("[Example] Channel chat example completed");
        }
    }
}