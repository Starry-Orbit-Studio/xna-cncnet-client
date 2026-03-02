using System.Threading.Tasks;

namespace ClientCore.ExternalAccount
{
    /// <summary>
    /// OAuth API客户端接口，负责与后端服务器进行OAuth认证相关的通信
    /// </summary>
    public interface IOAuthApiClient
    {
        /// <summary>
        /// 获取OAuth授权URL
        /// </summary>
        /// <param name="provider">认证提供商（github, qq等）</param>
        /// <param name="localPort">本地回调端口（某些提供商需要，如QQ）</param>
        /// <returns>授权URL</returns>
        Task<string> GetAuthorizationUrlAsync(string provider, int localPort = 12345);

        /// <summary>
        /// 使用授权码交换访问令牌
        /// </summary>
        /// <param name="provider">认证提供商</param>
        /// <param name="code">授权码</param>
        /// <param name="state">状态参数</param>
        /// <returns>访问令牌</returns>
        Task<string> ExchangeCodeForTokenAsync(string provider, string code, string state);
    }
}