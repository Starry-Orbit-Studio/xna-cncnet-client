#nullable enable
using System.Threading.Tasks;

namespace DTAClient.Online.DomainAction
{
    /// <summary>
    /// Domain-Action 消息处理器接口
    /// </summary>
    public interface IDomainActionHandler
    {
        /// <summary>
        /// 处理器负责的领域
        /// </summary>
        string Domain { get; }
        
        /// <summary>
        /// 检查是否能处理指定动作
        /// </summary>
        bool CanHandle(string action);
        
        /// <summary>
        /// 处理消息
        /// </summary>
        Task HandleAsync(DomainActionMessage message);
    }
}