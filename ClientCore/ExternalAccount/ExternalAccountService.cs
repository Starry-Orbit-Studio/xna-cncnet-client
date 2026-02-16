using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Rampastring.Tools;
using ClientCore;
using ClientCore.Extensions;

namespace ClientCore.ExternalAccount
{
    /// <summary>
    /// 外部账户服务，处理用户登录、令牌管理和API调用
    /// </summary>
    public class ExternalAccountService
    {
        private const string TOKEN_SECTION = "ExternalAccount";
        private const string TOKEN_KEY = "AuthToken";
        private const string REFRESH_TOKEN_KEY = "RefreshToken";
        private const string USER_INFO_KEY = "UserInfo";

        private readonly HttpClient _httpClient;
        private readonly Rampastring.Tools.IniFile _settingsIni;

        private string _authToken;
        private string _refreshToken;
        private UserInfo _userInfo;
        
        private string _loginEndpoint = "api/auth/login";
        private string _refreshEndpoint = "api/auth/refresh";
        private string _userInfoEndpoint = "users/me";
        private string _updateProfileEndpoint = "api/v1/users/me";
        private string _linkProviderEndpoint = "api/v1/link";
        private string _unlinkEndpoint = "api/v1/auth/unlink";

        public event EventHandler LoginStateChanged;
        public event EventHandler UserInfoUpdated;
        
        /// <summary>
        /// 获取最后一次操作的错误信息
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// 获取当前用户信息，如果未登录则为null
        /// </summary>
        public UserInfo CurrentUser => _userInfo;

        /// <summary>
        /// 获取是否已登录
        /// </summary>
        public bool IsLoggedIn => !string.IsNullOrEmpty(_authToken) && _userInfo != null;

        public ExternalAccountService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(GetUserAgent());
            
            // 从UserINISettings的INI文件中加载令牌
            string settingsPath = Rampastring.Tools.SafePath.CombineFilePath(ProgramConstants.GamePath, 
                ClientConfiguration.Instance.SettingsIniName);
            _settingsIni = new Rampastring.Tools.IniFile(settingsPath);
            
            LoadTokens();
            
            // 自动设置API基础地址
            InitializeBaseAddress();
        }

        private void InitializeBaseAddress()
        {
            try
            {
                string apiBaseUrl = ClientConfiguration.Instance.ExternalAccountApiBaseUrl;
                if (!string.IsNullOrEmpty(apiBaseUrl))
                {
                    SetBaseAddress(apiBaseUrl);
                    Rampastring.Tools.Logger.Log($"ExternalAccountService: 自动设置BaseAddress为 {apiBaseUrl}");
                }
            }
            catch (Exception ex)
            {
                Rampastring.Tools.Logger.Log($"ExternalAccountService: 设置BaseAddress失败 - {ex.Message}");
            }
        }

        private string GetUserAgent()
        {
            return $"CnCNetClient/1.0.0";
        }

        /// <summary>
        /// 设置API基础地址
        /// </summary>
        public void SetBaseAddress(string baseAddress)
        {
            if (!Uri.TryCreate(baseAddress, UriKind.Absolute, out Uri uri))
                throw new ArgumentException("无效的BaseAddress");
                
            // 确保BaseAddress以斜杠结尾，以便正确组合相对路径
            if (!uri.AbsoluteUri.EndsWith("/"))
            {
                uri = new Uri(uri.AbsoluteUri + "/");
                Rampastring.Tools.Logger.Log($"ExternalAccountService.SetBaseAddress: 调整为以斜杠结尾: {uri}");
            }
                
            _httpClient.BaseAddress = uri;
            Rampastring.Tools.Logger.Log($"ExternalAccountService.SetBaseAddress: 设置为 {uri}");
        }

        /// <summary>
        /// 配置API端点
        /// </summary>
        public void ConfigureEndpoints(string loginEndpoint, string refreshEndpoint, string userInfoEndpoint)
        {
            if (!string.IsNullOrEmpty(loginEndpoint))
            {
                // 如果 loginEndpoint 不是绝对URL且以斜杠开头，移除前导斜杠以确保正确组合
                if (!Uri.IsWellFormedUriString(loginEndpoint, UriKind.Absolute) && loginEndpoint.StartsWith("/"))
                {
                    _loginEndpoint = loginEndpoint.Substring(1);
                    Rampastring.Tools.Logger.Log($"ExternalAccountService.ConfigureEndpoints: _loginEndpoint 调整为 {_loginEndpoint} (移除前导斜杠)");
                }
                else
                {
                    _loginEndpoint = loginEndpoint;
                    Rampastring.Tools.Logger.Log($"ExternalAccountService.ConfigureEndpoints: _loginEndpoint 设置为 {loginEndpoint}");
                }
            }
            if (!string.IsNullOrEmpty(refreshEndpoint))
            {
                // 如果 refreshEndpoint 不是绝对URL且以斜杠开头，移除前导斜杠以确保正确组合
                if (!Uri.IsWellFormedUriString(refreshEndpoint, UriKind.Absolute) && refreshEndpoint.StartsWith("/"))
                {
                    _refreshEndpoint = refreshEndpoint.Substring(1);
                    Rampastring.Tools.Logger.Log($"ExternalAccountService.ConfigureEndpoints: _refreshEndpoint 调整为 {_refreshEndpoint} (移除前导斜杠)");
                }
                else
                {
                    _refreshEndpoint = refreshEndpoint;
                    Rampastring.Tools.Logger.Log($"ExternalAccountService.ConfigureEndpoints: _refreshEndpoint 设置为 {refreshEndpoint}");
                }
            }
            if (!string.IsNullOrEmpty(userInfoEndpoint))
            {
                // 如果 userInfoEndpoint 不是绝对URL且以斜杠开头，移除前导斜杠以确保正确组合
                if (!Uri.IsWellFormedUriString(userInfoEndpoint, UriKind.Absolute) && userInfoEndpoint.StartsWith("/"))
                {
                    _userInfoEndpoint = userInfoEndpoint.Substring(1);
                    Rampastring.Tools.Logger.Log($"ExternalAccountService.ConfigureEndpoints: _userInfoEndpoint 调整为 {_userInfoEndpoint} (移除前导斜杠)");
                }
                else
                {
                    _userInfoEndpoint = userInfoEndpoint;
                    Rampastring.Tools.Logger.Log($"ExternalAccountService.ConfigureEndpoints: _userInfoEndpoint 设置为 {userInfoEndpoint}");
                }
            }
        }

        /// <summary>
        /// 使用用户名和密码登录
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            LastError = null;
            try
            {
                var request = new LoginRequest
                {
                    Username = username,
                    Password = password
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_loginEndpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"服务器返回错误: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var loginResult = JsonSerializer.Deserialize<LoginResponse>(responseJson);

                _authToken = loginResult.AccessToken;
                _refreshToken = loginResult.RefreshToken;
                
                // 获取用户信息
                await FetchUserInfoAsync();
                
                SaveTokens();
                
                LoginStateChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"登录失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"登录过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"登录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Logout()
        {
            _authToken = null;
            _refreshToken = null;
            _userInfo = null;
            UserInfoUpdated?.Invoke(this, EventArgs.Empty);
            
            ClearTokens();
            
            LoginStateChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 刷新访问令牌
        /// </summary>
        public async Task<bool> RefreshTokenAsync()
        {
            LastError = null;
            if (string.IsNullOrEmpty(_refreshToken))
            {
                LastError = "刷新令牌不存在";
                return false;
            }

            try
            {
                var request = new RefreshTokenRequest
                {
                    RefreshToken = _refreshToken
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_refreshEndpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"刷新令牌失败: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var refreshResult = JsonSerializer.Deserialize<LoginResponse>(responseJson);

                _authToken = refreshResult.AccessToken;
                _refreshToken = refreshResult.RefreshToken;
                
                SaveTokens();
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"刷新令牌失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"刷新令牌过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"刷新令牌失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        private async Task<bool> FetchUserInfoAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, _userInfoEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

                Rampastring.Tools.Logger.Log($"FetchUserInfoAsync: BaseAddress={_httpClient.BaseAddress}, _userInfoEndpoint={_userInfoEndpoint}");
                string fullUrl = _httpClient.BaseAddress == null ? _userInfoEndpoint : new Uri(_httpClient.BaseAddress, _userInfoEndpoint).ToString();
                Rampastring.Tools.Logger.Log($"FetchUserInfoAsync: 发送请求到 {fullUrl}");

                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"获取用户信息失败: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                Rampastring.Tools.Logger.Log($"FetchUserInfoAsync: 收到响应: {responseJson}");
                _userInfo = JsonSerializer.Deserialize<UserInfo>(responseJson);
                Rampastring.Tools.Logger.Log($"FetchUserInfoAsync: 用户信息反序列化成功 - Nickname: {_userInfo?.Nickname}, Avatar: {_userInfo?.Avatar}");
                
                UserInfoUpdated?.Invoke(this, EventArgs.Empty);
                SaveTokens();
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"获取用户信息失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"获取用户信息过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"获取用户信息失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 使用OAuth令牌登录
        /// </summary>
        public async Task<bool> LoginWithOAuthAsync(string accessToken, UserInfo userInfo = null)
        {
            LastError = null;
            try
            {
                _authToken = accessToken;
                
                if (userInfo != null)
                {
                    _userInfo = userInfo;
                    UserInfoUpdated?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // 如果没有提供用户信息，尝试从API获取
                    bool fetchSuccess = await FetchUserInfoAsync();
                    if (!fetchSuccess)
                    {
                        Rampastring.Tools.Logger.Log($"LoginWithOAuthAsync: 获取用户信息失败 - {LastError}");
                        return false;
                    }
                }
                
                SaveTokens();
                
                Rampastring.Tools.Logger.Log($"LoginWithOAuthAsync: 触发LoginStateChanged事件，订阅者数量: {LoginStateChanged?.GetInvocationList()?.Length ?? 0}");
                LoginStateChanged?.Invoke(this, EventArgs.Empty);
                Rampastring.Tools.Logger.Log($"LoginWithOAuthAsync: 登录成功，用户: {_userInfo?.Nickname ?? "未知"}");
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"OAuth登录失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"OAuth登录失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送授权API请求
        /// </summary>
        public async Task<HttpResponseMessage> SendAuthorizedRequestAsync(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(_authToken))
                throw new InvalidOperationException("用户未登录");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            var response = await _httpClient.SendAsync(request);
            
            // 如果令牌过期，尝试刷新并重试一次
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                bool refreshed = await RefreshTokenAsync();
                if (refreshed)
                {
                    // 更新请求的授权头
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                    response = await _httpClient.SendAsync(request);
                }
            }
            
            return response;
        }

        /// <summary>
        /// 加载保存的令牌和用户信息
        /// </summary>
        private void LoadTokens()
        {
            _authToken = _settingsIni.GetStringValue(TOKEN_SECTION, TOKEN_KEY, string.Empty);
            _refreshToken = _settingsIni.GetStringValue(TOKEN_SECTION, REFRESH_TOKEN_KEY, string.Empty);
            
            string userInfoJson = _settingsIni.GetStringValue(TOKEN_SECTION, USER_INFO_KEY, string.Empty);
            if (!string.IsNullOrEmpty(userInfoJson))
            {
                try
                {
                    _userInfo = JsonSerializer.Deserialize<UserInfo>(userInfoJson);
                }
                catch (JsonException)
                {
                    _userInfo = null;
                }
            }
        }

        /// <summary>
        /// 保存令牌和用户信息
        /// </summary>
        private void SaveTokens()
        {
            _settingsIni.SetStringValue(TOKEN_SECTION, TOKEN_KEY, _authToken ?? string.Empty);
            _settingsIni.SetStringValue(TOKEN_SECTION, REFRESH_TOKEN_KEY, _refreshToken ?? string.Empty);
            
            if (_userInfo != null)
            {
                string userInfoJson = JsonSerializer.Serialize(_userInfo);
                _settingsIni.SetStringValue(TOKEN_SECTION, USER_INFO_KEY, userInfoJson);
            }
            else
            {
                _settingsIni.SetStringValue(TOKEN_SECTION, USER_INFO_KEY, string.Empty);
            }
            
            _settingsIni.WriteIniFile();
        }

        /// <summary>
        /// 清除保存的令牌
        /// </summary>
        private void ClearTokens()
        {
            _settingsIni.SetStringValue(TOKEN_SECTION, TOKEN_KEY, string.Empty);
            _settingsIni.SetStringValue(TOKEN_SECTION, REFRESH_TOKEN_KEY, string.Empty);
            _settingsIni.SetStringValue(TOKEN_SECTION, USER_INFO_KEY, string.Empty);
            _settingsIni.WriteIniFile();
        }

        /// <summary>
        /// 更新用户资料
        /// </summary>
        public async Task<bool> UpdateProfileAsync(string nickname, string avatar = null)
        {
            LastError = null;
            if (string.IsNullOrEmpty(_authToken))
            {
                LastError = "用户未登录";
                return false;
            }

            try
            {
                var request = new UpdateProfileRequest
                {
                    Nickname = nickname,
                    Avatar = avatar
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Put, _updateProfileEndpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"更新用户资料失败: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                _userInfo = JsonSerializer.Deserialize<UserInfo>(responseJson);

                UserInfoUpdated?.Invoke(this, EventArgs.Empty);
                SaveTokens();
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"更新用户资料失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"更新用户资料过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"更新用户资料失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 绑定第三方账号
        /// </summary>
        public async Task<bool> LinkProviderAsync(string provider, string code, string state)
        {
            LastError = null;
            if (string.IsNullOrEmpty(_authToken))
            {
                LastError = "用户未登录";
                return false;
            }

            try
            {
                var request = new LinkProviderRequest
                {
                    Code = code,
                    State = state
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var endpoint = $"{_linkProviderEndpoint}/{provider}";
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);

                if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
                {
                    LastError = $"绑定账号失败: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                await FetchUserInfoAsync();
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"绑定账号失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"绑定账号过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"绑定账号失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 按 provider 解绑第三方账号
        /// </summary>
        public async Task<bool> UnlinkProviderByProviderAsync(string provider)
        {
            LastError = null;
            if (string.IsNullOrEmpty(_authToken))
            {
                LastError = "用户未登录";
                return false;
            }

            try
            {
                var endpoint = $"{_unlinkEndpoint}/provider/{provider}";
                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, endpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);

                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"解绑账号失败: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                await FetchUserInfoAsync();
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"解绑账号失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"解绑账号过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"解绑账号失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 按 identity_id 解绑第三方账号
        /// </summary>
        public async Task<bool> UnlinkProviderByIdentityAsync(int identityId)
        {
            LastError = null;
            if (string.IsNullOrEmpty(_authToken))
            {
                LastError = "用户未登录";
                return false;
            }

            try
            {
                var request = new UnlinkProviderRequest
                {
                    IdentityId = identityId
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var httpRequest = new HttpRequestMessage(HttpMethod.Delete, _unlinkEndpoint);
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
                httpRequest.Content = content;

                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    LastError = $"解绑账号失败: {(int)response.StatusCode} {response.ReasonPhrase}";
                    return false;
                }

                await FetchUserInfoAsync();
                return true;
            }
            catch (HttpRequestException ex)
            {
                LastError = $"网络连接失败: {ex.Message}";
                Rampastring.Tools.Logger.Log($"解绑账号失败: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"解绑账号过程出错: {ex.Message}";
                Rampastring.Tools.Logger.Log($"解绑账号失败: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// 登录请求
    /// </summary>
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>
    /// 登录响应
    /// </summary>
    public class LoginResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// 刷新令牌请求
    /// </summary>
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }

    /// <summary>
    /// 更新用户资料请求
    /// </summary>
    public class UpdateProfileRequest
    {
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }
    }

    /// <summary>
    /// 绑定第三方账号请求
    /// </summary>
    public class LinkProviderRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }
    }

    /// <summary>
    /// 解绑第三方账号请求
    /// </summary>
    public class UnlinkProviderRequest
    {
        [JsonPropertyName("identity_id")]
        public int IdentityId { get; set; }
    }

    /// <summary>
    /// 身份信息（OAuth提供商身份）
    /// </summary>
    public class IdentityInfo
    {
        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("provider_uid")]
        public string IdentityId { get; set; }

        [JsonPropertyName("profile_url")]
        public string ProfileUrl { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }
    }

    /// <summary>
    /// 用户信息
    /// </summary>
    public class UserInfo
    {
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; }

        [JsonPropertyName("avatar")]
        public string Avatar { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("experience_points")]
        public int ExperiencePoints { get; set; }

        [JsonPropertyName("last_login_at")]
        public DateTime? LastLoginAt { get; set; }

        [JsonPropertyName("identities")]
        public List<IdentityInfo> Identities { get; set; } = new List<IdentityInfo>();

        [JsonPropertyName("online_status")]
        public string OnlineStatus { get; set; }

        // 向后兼容的属性
        [JsonIgnore]
        public string Username => Nickname;

        [JsonIgnore]
        public string DisplayName => Nickname;

        [JsonIgnore]
        public string AvatarUrl => Avatar;

        [JsonIgnore]
        public string Email => Identities?.FirstOrDefault(i => i.Provider == "email")?.IdentityId ?? string.Empty;
    }
}