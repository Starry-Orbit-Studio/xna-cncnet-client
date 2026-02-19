using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Rampastring.Tools;

namespace ClientCore.ExternalAccount
{
    /// <summary>
    /// 安全的令牌存储服务，使用Windows DPAPI加密存储敏感数据
    /// </summary>
    public class SecureTokenStorage
    {
        private const string TOKEN_FILE_NAME = "secure_tokens.dat";
        private readonly string _storagePath;

        public SecureTokenStorage(string gamePath)
        {
            _storagePath = SafePath.CombineFilePath(gamePath, TOKEN_FILE_NAME);
        }

        /// <summary>
        /// 保存令牌数据（加密存储）
        /// </summary>
        public void SaveTokens(string accessToken, string refreshToken, UserInfo userInfo)
        {
            try
            {
                var tokenData = new TokenData
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    UserInfo = userInfo,
                    SavedAt = DateTime.UtcNow
                };

                string json = JsonSerializer.Serialize(tokenData);
                byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

                byte[] encryptedBytes = ProtectData(jsonBytes);

                File.WriteAllBytes(_storagePath, encryptedBytes);
                Logger.Log("SecureTokenStorage: 令牌已加密保存");
            }
            catch (Exception ex)
            {
                Logger.Log($"SecureTokenStorage: 保存令牌失败 - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载令牌数据（解密）
        /// </summary>
        public TokenData LoadTokens()
        {
            try
            {
                if (!File.Exists(_storagePath))
                {
                    Logger.Log("SecureTokenStorage: 令牌文件不存在");
                    return null;
                }

                byte[] encryptedBytes = File.ReadAllBytes(_storagePath);

                byte[] decryptedBytes = UnprotectData(encryptedBytes);

                string json = Encoding.UTF8.GetString(decryptedBytes);
                var tokenData = JsonSerializer.Deserialize<TokenData>(json);

                Logger.Log($"SecureTokenStorage: 成功加载令牌，用户: {tokenData?.UserInfo?.Nickname ?? "未知"}");
                return tokenData;
            }
            catch (Exception ex)
            {
                Logger.Log($"SecureTokenStorage: 加载令牌失败 - {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 清除保存的令牌
        /// </summary>
        public void ClearTokens()
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    File.Delete(_storagePath);
                    Logger.Log("SecureTokenStorage: 令牌已清除");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"SecureTokenStorage: 清除令牌失败 - {ex.Message}");
            }
        }

        /// <summary>
        /// 检查是否存在保存的令牌
        /// </summary>
        public bool HasStoredTokens()
        {
            return File.Exists(_storagePath);
        }

        private byte[] ProtectData(byte[] data)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsDPAPI.Protect(data, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                throw new PlatformNotSupportedException("DPAPI only supported on Windows");
            }
        }

        private byte[] UnprotectData(byte[] encryptedData)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return WindowsDPAPI.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
            }
            else
            {
                throw new PlatformNotSupportedException("DPAPI only supported on Windows");
            }
        }
    }

    /// <summary>
    /// 令牌数据模型
    /// </summary>
    public class TokenData
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public UserInfo UserInfo { get; set; }
        public DateTime SavedAt { get; set; }
    }

    /// <summary>
    /// Windows DPAPI 封装
    /// </summary>
    internal static class WindowsDPAPI
    {
        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern bool CryptProtectData(
            ref DATA_BLOB pDataIn,
            string szDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct,
            int dwFlags,
            out DATA_BLOB pDataOut);

        [DllImport("crypt32.dll", SetLastError = true)]
        private static extern bool CryptUnprotectData(
            ref DATA_BLOB pDataIn,
            string szDataDescr,
            ref DATA_BLOB pOptionalEntropy,
            IntPtr pvReserved,
            ref CRYPTPROTECT_PROMPTSTRUCT pPromptStruct,
            int dwFlags,
            out DATA_BLOB pDataOut);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [StructLayout(LayoutKind.Sequential)]
        private struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPTPROTECT_PROMPTSTRUCT
        {
            public int cbSize;
            public int dwPromptFlags;
            public IntPtr hwndApp;
            public string szPrompt;
        }

        private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        public static byte[] Protect(byte[] data, byte[] optionalEntropy, DataProtectionScope scope)
        {
            DATA_BLOB input = new DATA_BLOB();
            DATA_BLOB entropy = new DATA_BLOB();
            DATA_BLOB output = new DATA_BLOB();

            try
            {
                input.cbData = data.Length;
                input.pbData = Marshal.AllocHGlobal(data.Length);
                Marshal.Copy(data, 0, input.pbData, data.Length);

                if (optionalEntropy != null)
                {
                    entropy.cbData = optionalEntropy.Length;
                    entropy.pbData = Marshal.AllocHGlobal(optionalEntropy.Length);
                    Marshal.Copy(optionalEntropy, 0, entropy.pbData, optionalEntropy.Length);
                }

                CRYPTPROTECT_PROMPTSTRUCT prompt = new CRYPTPROTECT_PROMPTSTRUCT();
                prompt.cbSize = Marshal.SizeOf(typeof(CRYPTPROTECT_PROMPTSTRUCT));

                int flags = CRYPTPROTECT_UI_FORBIDDEN;
                if (scope == DataProtectionScope.LocalMachine)
                    flags |= 0x1;

                bool success = CryptProtectData(ref input, null, ref entropy, IntPtr.Zero, ref prompt, flags, out output);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(error, "CryptProtectData failed");
                }

                byte[] result = new byte[output.cbData];
                Marshal.Copy(output.pbData, result, 0, output.cbData);

                return result;
            }
            finally
            {
                if (input.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(input.pbData);
                if (entropy.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(entropy.pbData);
                if (output.pbData != IntPtr.Zero)
                    LocalFree(output.pbData);
            }
        }

        public static byte[] Unprotect(byte[] encryptedData, byte[] optionalEntropy, DataProtectionScope scope)
        {
            DATA_BLOB input = new DATA_BLOB();
            DATA_BLOB entropy = new DATA_BLOB();
            DATA_BLOB output = new DATA_BLOB();

            try
            {
                input.cbData = encryptedData.Length;
                input.pbData = Marshal.AllocHGlobal(encryptedData.Length);
                Marshal.Copy(encryptedData, 0, input.pbData, encryptedData.Length);

                if (optionalEntropy != null)
                {
                    entropy.cbData = optionalEntropy.Length;
                    entropy.pbData = Marshal.AllocHGlobal(optionalEntropy.Length);
                    Marshal.Copy(optionalEntropy, 0, entropy.pbData, optionalEntropy.Length);
                }

                CRYPTPROTECT_PROMPTSTRUCT prompt = new CRYPTPROTECT_PROMPTSTRUCT();
                prompt.cbSize = Marshal.SizeOf(typeof(CRYPTPROTECT_PROMPTSTRUCT));

                int flags = CRYPTPROTECT_UI_FORBIDDEN;
                if (scope == DataProtectionScope.LocalMachine)
                    flags |= 0x1;

                bool success = CryptUnprotectData(ref input, null, ref entropy, IntPtr.Zero, ref prompt, flags, out output);

                if (!success)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new System.ComponentModel.Win32Exception(error, "CryptUnprotectData failed");
                }

                byte[] result = new byte[output.cbData];
                Marshal.Copy(output.pbData, result, 0, output.cbData);

                return result;
            }
            finally
            {
                if (input.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(input.pbData);
                if (entropy.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(entropy.pbData);
                if (output.pbData != IntPtr.Zero)
                    LocalFree(output.pbData);
            }
        }
    }

    /// <summary>
    /// 数据保护范围
    /// </summary>
    public enum DataProtectionScope
    {
        CurrentUser = 0,
        LocalMachine = 1
    }
}
