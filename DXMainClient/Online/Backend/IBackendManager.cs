#nullable enable
using System;
using System.Threading.Tasks;
using DTAClient.Online.EventArguments;
using DTAClient.Online.Backend.EventArguments;

namespace DTAClient.Online.Backend
{
    /// <summary>
    /// 扩展的 IConnectionManager 接口，包含 BackendManager 特有的功能
    /// 用于支持 Domain-Action 协议适配器
    /// </summary>
    public interface IBackendManager : IConnectionManager
    {
        /// <summary>
        /// 房间创建事件
        /// </summary>
        event EventHandler<RoomCreatedEventArgs>? RoomCreated;
        
        /// <summary>
        /// 房间更新事件
        /// </summary>
        event EventHandler<RoomUpdatedEventArgs>? RoomUpdated;
        
        /// <summary>
        /// 房间删除事件
        /// </summary>
        event EventHandler<RoomDeletedEventArgs>? RoomDeleted;
        
        /// <summary>
        /// 空间管理器，用于创建和管理房间
        /// </summary>
        BackendSpaceManager SpaceManager { get; }
        
        /// <summary>
        /// 异步连接到后端服务器
        /// </summary>
        Task ConnectAsync();
    }
}