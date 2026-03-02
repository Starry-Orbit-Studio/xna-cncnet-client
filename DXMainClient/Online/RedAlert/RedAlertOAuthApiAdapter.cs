using System.Threading.Tasks;
using ClientCore.ExternalAccount;

namespace DTAClient.Online.RedAlert
{
    /// <summary>
    /// RedAlert OAuth API适配器，实现IOAuthApiClient接口
    /// 使用RedAlertApiClient进行实际的API调用
    /// </summary>
    public class RedAlertOAuthApiAdapter : IOAuthApiClient
    {
        private readonly RedAlertApiClient _apiClient;

        public RedAlertOAuthApiAdapter(RedAlertApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        /// <summary>
        /// 获取OAuth授权URL
        /// </summary>
        public async Task<string> GetAuthorizationUrlAsync(string provider, int localPort = 12345)
        {
            var response = await _apiClient.StartOAuthAsync(provider);
            
            // 对于QQ等需要本地端口的提供商，在URL后添加查询参数
            if (provider == "qq" && !string.IsNullOrEmpty(response.RedirectUrl))
            {
                // 检查URL是否已包含查询参数
                var separator = response.RedirectUrl.Contains('?') ? '&' : '?';
                return $"{response.RedirectUrl}{separator}local_port={localPort}";
            }
            
            return response.RedirectUrl;
        }

        /// <summary>
        /// 使用授权码交换访问令牌
        /// </summary>
        public async Task<string> ExchangeCodeForTokenAsync(string provider, string code, string state)
        {
            var response = await _apiClient.LoginWithOAuthAsync(provider, code, state);
            return response.AccessToken;
        }
    }
}