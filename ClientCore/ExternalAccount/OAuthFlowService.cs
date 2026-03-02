#nullable enable

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Rampastring.Tools;

namespace ClientCore.ExternalAccount
{
    /// <summary>
    /// OAuth流程服务，处理后端服务器中转的OAuth 2.0认证流程
    /// 支持GitHub、QQ等第三方登录
    /// </summary>
    public class OAuthFlowService
    {
        private const string LOCAL_CALLBACK_URL = "http://127.0.0.1:12345/callback/";
        private const int LOCAL_PORT = 12345;

        private readonly string _apiBaseUrl;
        private readonly string _provider;
        private readonly IOAuthApiClient? _apiClient;

        private HttpListener? _httpListener;

        /// <summary>
        /// 获取或设置最后一次错误信息
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// 认证完成时触发的事件
        /// </summary>
        public event EventHandler<OAuthFlowResult> AuthenticationCompleted;

        public OAuthFlowService(string apiBaseUrl, string provider = "github")
        {
            _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _httpListener = new HttpListener();
            LastError = string.Empty;
            AuthenticationCompleted = delegate { };
        }

        /// <summary>
        /// 使用IOAuthApiClient的新构造函数
        /// </summary>
        public OAuthFlowService(IOAuthApiClient apiClient, string provider = "github")
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _apiBaseUrl = string.Empty; // 设置为空字符串，表示使用API客户端
            _httpListener = new HttpListener();
            LastError = string.Empty;
            AuthenticationCompleted = delegate { };
        }



        /// <summary>
        /// 启动OAuth认证流程
        /// </summary>
        public async Task StartAuthenticationAsync()
        {
            LastError = string.Empty;

            try
            {
                // 1. 启动本地HTTP服务器监听回调
                await StartHttpListenerAsync();

                // 2. 从后端服务器获取授权URL
                string authUrl = await GetAuthorizationUrlAsync(LOCAL_PORT);

                // 3. 打开浏览器到授权页面
                OpenBrowser(authUrl);

                // 4. HTTP服务器将在后台处理回调
            }
            catch (Exception ex)
            {
                LastError = $"启动认证流程失败: {ex.Message}";
                throw;
            }
        }

        /// <summary>
        /// 从后端服务器获取授权URL
        /// </summary>
        private async Task<string> GetAuthorizationUrlAsync(int localPort = 12345)
        {
            try
            {
                // 如果配置了API客户端，使用接口
                Logger.Log($"[OAuthFlowService] GetAuthorizationUrlAsync: _apiClient is {(_apiClient != null ? "not null" : "null")}, _provider={_provider}, _apiBaseUrl={_apiBaseUrl}");
                if (_apiClient != null)
                {
                    Logger.Log($"[OAuthFlowService] Using IOAuthApiClient implementation");
                    return await _apiClient.GetAuthorizationUrlAsync(_provider, localPort);
                }

                // 否则使用旧的HTTP调用（向后兼容）
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CnCNetClient");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                // 构建URL路径 - 新后端API需要添加/auth前缀
                string url = $"{_apiBaseUrl}/auth/start/{_provider}";
                if (_provider == "qq")
                {
                    url += $"?local_port={localPort}";
                }
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"获取授权URL失败: {(int)response.StatusCode} {response.ReasonPhrase} {url}");
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                Logger.Log($"{responseJson}");
                var authResponse = JsonSerializer.Deserialize<OAuthFlowAuthURLResponse>(responseJson);

                if (authResponse == null || string.IsNullOrEmpty(authResponse.AuthorizationUrl))
                {
                    throw new Exception("从服务器返回的授权URL为空");
                }

                Logger.Log($"获取到授权URL: {authResponse.AuthorizationUrl}");
                return authResponse.AuthorizationUrl!;
            }
            catch (Exception ex)
            {
                LastError = $"获取授权URL失败: {ex.Message}";
                Logger.Log($"获取授权URL失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 启动HTTP监听器处理OAuth回调
        /// </summary>
        private async Task StartHttpListenerAsync()
        {
            if (_httpListener != null)
            {
                try
                {
                    if (_httpListener.IsListening)
                        _httpListener.Stop();
                    _httpListener.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log($"停止旧HTTP监听器时出错: {ex.Message}");
                }
                finally
                {
                    _httpListener = null;
                }
            }

            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add(LOCAL_CALLBACK_URL);

            try
            {
                _httpListener.Start();
                _ = Task.Run(() => HandleIncomingRequestsAsync());
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5) // 访问被拒绝
            {
                LastError = $"无法启动HTTP监听器: 访问被拒绝。可能需要管理员权限或在Windows防火墙中添加例外。";
                Logger.Log($"HTTP监听器启动失败(访问被拒绝): {ex.Message}");
                throw new InvalidOperationException(LastError, ex);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 48 || ex.ErrorCode == 10048) // 地址已在使用中
            {
                LastError = $"端口 {LOCAL_PORT} 已被其他程序占用。请关闭使用该端口的程序(如其他服务器实例)，或修改配置使用其他端口。";
                Logger.Log($"HTTP监听器启动失败(端口占用): {ex.Message}");
                throw new InvalidOperationException(LastError, ex);
            }
            catch (Exception ex)
            {
                LastError = $"无法启动HTTP监听器: {ex.Message}";
                Logger.Log($"HTTP监听器启动失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 处理传入的HTTP请求
        /// </summary>
        private async Task HandleIncomingRequestsAsync()
        {
            while (_httpListener?.IsListening == true)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    Logger.Log($"收到HTTP请求: {context.Request.Url}");
                    await ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    // 监听器已停止
                    break;
                }
                catch (Exception ex)
                {
                    LastError = $"处理请求时出错: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 处理单个HTTP请求
        /// </summary>
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // 检查是否为OAuth回调 - 支持 /callback 和 /callback/ 两种路径
            string localPath = request.Url?.LocalPath ?? string.Empty;
            bool isCallbackPath = localPath.Equals("/callback", StringComparison.OrdinalIgnoreCase) ||
                                  localPath.Equals("/callback/", StringComparison.OrdinalIgnoreCase);

            try
            {
                Logger.Log($"处理HTTP请求: 路径={localPath}, 完整URL={request.Url}, 回调路径={isCallbackPath}");

                if (isCallbackPath)
                {
                    string? code = request.QueryString["code"];
                    string? state = request.QueryString["state"];
                    string? error = request.QueryString["error"];

                    Logger.Log($"回调参数: code={code}, state={state}, error={error}");

                    if (!string.IsNullOrEmpty(error))
                    {
                        string? errorDescription = request.QueryString["error_description"];
                        LastError = $"OAuth授权失败: {error} - {errorDescription}";

                        // 返回错误页面
                        byte[] errorResponse = Encoding.UTF8.GetBytes(
                            "<html><body><h1>Authentication Failed</h1><p id='message'>Please return to the application. This window will close automatically in 3 seconds.</p><script>setTimeout(function() { window.close(); setTimeout(function() { document.getElementById('message').innerHTML = 'You can now safely close this window.'; }, 1000); }, 3000);</script></body></html>");
                        response.ContentType = "text/html";
                        response.ContentLength64 = errorResponse.Length;
                        await response.OutputStream.WriteAsync(errorResponse, 0, errorResponse.Length);

                        AuthenticationCompleted?.Invoke(this, new OAuthFlowResult
                        {
                            Success = false,
                            Error = LastError
                        });
                    }
                    else if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
                    {
                        // 用授权码交换访问令牌
                        var tokenResult = await ExchangeCodeForTokenAsync(code!, state!);

                        if (tokenResult.Success)
                        {
                            // 返回成功页面
                            byte[] successResponse = Encoding.UTF8.GetBytes(
                                "<html><body><h1>Authentication Successful</h1><p id='message'>You can now return to the application. This window will close automatically in 3 seconds.</p><script>setTimeout(function() { window.close(); setTimeout(function() { document.getElementById('message').innerHTML = 'You can now safely close this window.'; }, 1000); }, 3000);</script></body></html>");
                            response.ContentType = "text/html";
                            response.ContentLength64 = successResponse.Length;
                            await response.OutputStream.WriteAsync(successResponse, 0, successResponse.Length);

                            AuthenticationCompleted?.Invoke(this, new OAuthFlowResult
                            {
                                Success = true,
                                AccessToken = tokenResult.AccessToken,
                                UserInfo = null // 用户信息由ExternalAccountService获取
                            });
                        }
                        else
                        {
                            // 返回错误页面
                            byte[] errorResponse = Encoding.UTF8.GetBytes(
                                $"<html><body><h1>Token Exchange Failed</h1><p>{tokenResult.Error}</p><p id='message'>This window will close automatically in 3 seconds.</p><script>setTimeout(function() {{ window.close(); setTimeout(function() {{ document.getElementById('message').innerHTML = 'You can now safely close this window.'; }}, 1000); }}, 3000);</script></body></html>");
                            response.ContentType = "text/html";
                            response.ContentLength64 = errorResponse.Length;
                            await response.OutputStream.WriteAsync(errorResponse, 0, errorResponse.Length);

                            AuthenticationCompleted?.Invoke(this, tokenResult);
                        }
                    }
                    else
                    {
                        // 参数不完整
                        byte[] errorResponse = Encoding.UTF8.GetBytes(
                            "<html><body><h1>Invalid Callback</h1><p>Missing code or state parameter.</p><p id='message'>This window will close automatically in 3 seconds.</p><script>setTimeout(function() { window.close(); setTimeout(function() { document.getElementById('message').innerHTML = 'You can now safely close this window.'; }, 1000); }, 3000);</script></body></html>");
                        response.ContentType = "text/html";
                        response.ContentLength64 = errorResponse.Length;
                        await response.OutputStream.WriteAsync(errorResponse, 0, errorResponse.Length);

                        AuthenticationCompleted?.Invoke(this, new OAuthFlowResult
                        {
                            Success = false,
                            Error = "回调参数不完整：缺少code或state参数"
                        });
                    }
                }
                else
                {
                    // 返回404
                    byte[] notFoundResponse = Encoding.UTF8.GetBytes(
                        "<html><body><h1>404 Not Found</h1></body></html>");
                    response.StatusCode = 404;
                    response.ContentType = "text/html";
                    response.ContentLength64 = notFoundResponse.Length;
                    await response.OutputStream.WriteAsync(notFoundResponse, 0, notFoundResponse.Length);
                }
            }
            finally
            {
                response.Close();

                // 成功处理回调后停止监听器
                if (isCallbackPath)
                {
                    _httpListener?.Stop();
                }
            }
        }

        /// <summary>
        /// 用授权码交换访问令牌
        /// </summary>
        private async Task<OAuthFlowResult> ExchangeCodeForTokenAsync(string code, string state)
        {
            try
            {
                // 如果配置了API客户端，使用接口
                if (_apiClient != null)
                {
                    string accessToken = await _apiClient.ExchangeCodeForTokenAsync(_provider, code, state);
                    return new OAuthFlowResult
                    {
                        Success = true,
                        AccessToken = accessToken
                    };
                }

                // 否则使用旧的HTTP调用（向后兼容）
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CnCNetClient");
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                Logger.Log($"交换令牌: code={code}, state={state}");

                var request = new OAuthFlowLoginRequest
                {
                    Code = code,
                    State = state
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // 构建登录URL路径 - 新后端API需要添加/auth前缀
                string url = $"{_apiBaseUrl}/auth/login/{_provider}";
                var response = await httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return new OAuthFlowResult
                    {
                        Success = false,
                        Error = $"Token exchange failed: {(int)response.StatusCode} {response.ReasonPhrase}"
                    };
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<OAuthFlowLoginResponse>(responseJson);

                if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                {
                    return new OAuthFlowResult
                    {
                        Success = false,
                        Error = "No access token in response"
                    };
                }

                return new OAuthFlowResult
                {
                    Success = true,
                    AccessToken = tokenResponse.AccessToken
                };
            }
            catch (Exception ex)
            {
                return new OAuthFlowResult
                {
                    Success = false,
                    Error = $"Token exchange error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 打开系统默认浏览器
        /// </summary>
        private void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LastError = $"无法打开浏览器: {ex.Message}";
                throw;
            }
        }

        /// <summary>
        /// 停止HTTP监听器
        /// </summary>
        public void Stop()
        {
            if (_httpListener != null)
            {
                try
                {
                    if (_httpListener.IsListening)
                        _httpListener.Stop();
                    _httpListener.Close();
                }
                catch (Exception ex)
                {
                    Logger.Log($"停止HTTP监听器时出错: {ex.Message}");
                }
                finally
                {
                    _httpListener = null;
                }
            }
        }
    }

    /// <summary>
    /// OAuth认证结果
    /// </summary>
    public class OAuthFlowResult
    {
        public bool Success { get; set; }
        public string? AccessToken { get; set; }
        public UserInfo? UserInfo { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// OAuth授权URL响应
    /// </summary>
    public class OAuthFlowAuthURLResponse
    {
        [JsonPropertyName("authorization_url")]
        public string? AuthorizationUrl { get; set; }
    }

    /// <summary>
    /// OAuth登录请求（用于code交换token）
    /// </summary>
    public class OAuthFlowLoginRequest
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("state")]
        public string State { get; set; } = string.Empty;
    }

    /// <summary>
    /// OAuth登录响应（包含访问令牌）
    /// </summary>
    public class OAuthFlowLoginResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}