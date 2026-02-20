#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ClientCore;
using ClientCore.Settings;
using DTAClient.Online.Backend.Models;
using Rampastring.Tools;

namespace DTAClient.Online.Backend
{
    public class GuestIdentityService
    {
        private static readonly object _lock = new object();
        private readonly Backend.BackendApiClient _apiClient;
        private string? _cachedAccessToken;

        public string? CachedAccessToken => _cachedAccessToken;

        public GuestIdentityService(Backend.BackendApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public string GetOrGenerateGuestUid()
        {
            lock (_lock)
            {
                string? storedUid = UserINISettings.Instance.GuestUid.Value;

                if (!string.IsNullOrEmpty(storedUid) && IsValidUuid(storedUid))
                {
                    Logger.Log($"[GuestIdentityService] Using stored guest_uid: {storedUid}");
                    return storedUid;
                }

                string newUid = GenerateUuid();
                UserINISettings.Instance.GuestUid.Value = newUid;
                UserINISettings.Instance.SaveSettings();

                Logger.Log($"[GuestIdentityService] Generated and saved new guest_uid: {newUid}");
                return newUid;
            }
        }

        public List<string> CollectHardwareIds()
        {
            var hwids = new List<string>();

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    CollectWindowsHardwareIds(hwids);
                }
                else
                {
                    CollectCrossPlatformHardwareIds(hwids);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Error collecting hardware IDs: {ex.Message}");
            }

            Logger.Log($"[GuestIdentityService] Collected {hwids.Count} hardware IDs");
            return hwids;
        }

        [SupportedOSPlatform("windows")]
        private void CollectWindowsHardwareIds(List<string> hwids)
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string? processorId = mo["ProcessorID"]?.ToString();
                    if (!string.IsNullOrEmpty(processorId))
                        hwids.Add($"CPU:{processorId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Failed to get CPU ID: {ex.Message}");
            }

            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string? serialNumber = mo["SerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(serialNumber) && serialNumber != "To Be Filled By O.E.M.")
                        hwids.Add($"MB:{serialNumber}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Failed to get motherboard ID: {ex.Message}");
            }

            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string? mac = nic.GetPhysicalAddress()?.ToString();
                        if (!string.IsNullOrEmpty(mac) && mac.Length >= 6)
                        {
                            hwids.Add($"MAC:{mac}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Failed to get MAC address: {ex.Message}");
            }

            try
            {
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string? serialNumber = mo["SerialNumber"]?.ToString();
                    if (!string.IsNullOrEmpty(serialNumber))
                    {
                        hwids.Add($"DISK:{serialNumber.Trim()}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Failed to get disk serial: {ex.Message}");
            }
        }

        private void CollectCrossPlatformHardwareIds(List<string> hwids)
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        string? mac = nic.GetPhysicalAddress()?.ToString();
                        if (!string.IsNullOrEmpty(mac) && mac.Length >= 6)
                        {
                            hwids.Add($"MAC:{mac}");
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Failed to get MAC address: {ex.Message}");
            }

            try
            {
                string machineIdPath = "/var/lib/dbus/machine-id";
                if (System.IO.File.Exists(machineIdPath))
                {
                    string machineId = System.IO.File.ReadAllText(machineIdPath).Trim();
                    if (!string.IsNullOrEmpty(machineId))
                        hwids.Add($"MACHINEID:{machineId}");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[GuestIdentityService] Failed to get machine-id: {ex.Message}");
            }
        }

        public async Task<string> LoginAsGuestAsync(string? nickname = null, int maxRetries = 3)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    string guestUid = GetOrGenerateGuestUid();
                    var hwidList = CollectHardwareIds();

                    var request = new GuestLoginRequest
                    {
                        GuestUid = guestUid,
                        Nickname = nickname ?? ProgramConstants.PLAYERNAME,
                        HwidList = hwidList
                    };

                    Logger.Log($"[GuestIdentityService] Attempting guest login (attempt {retryCount + 1}/{maxRetries})");

                    var response = await _apiClient.LoginAsGuestAsync(request);

                    _cachedAccessToken = response.AccessToken;
                    _apiClient.SetAccessToken(response.AccessToken);

                    Logger.Log($"[GuestIdentityService] Guest login successful");
                    return response.AccessToken;
                }
                catch (BackendApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    retryCount++;
                    Logger.Log($"[GuestIdentityService] Guest UID conflict detected (attempt {retryCount}/{maxRetries}), regenerating...");

                    if (retryCount >= maxRetries)
                    {
                        Logger.Log($"[GuestIdentityService] Max retries reached, giving up");
                        throw new InvalidOperationException("Failed to login as guest after maximum retries due to UID conflicts");
                    }

                    RegenerateAndStoreNewGuestUid();
                }
                catch (Exception ex)
                {
                    Logger.Log($"[GuestIdentityService] Guest login failed: {ex.Message}");
                    throw;
                }
            }

            throw new InvalidOperationException("Failed to login as guest");
        }

        public void RegenerateAndStoreNewGuestUid()
        {
            lock (_lock)
            {
                string newUid = GenerateUuid();
                UserINISettings.Instance.GuestUid.Value = newUid;
                UserINISettings.Instance.SaveSettings();

                Logger.Log($"[GuestIdentityService] Regenerated guest_uid: {newUid}");
            }
        }

        private string GenerateUuid()
        {
            byte[] uuidBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(uuidBytes);
            }

            uuidBytes[6] = (byte)((uuidBytes[6] & 0x0F) | 0x40);
            uuidBytes[8] = (byte)((uuidBytes[8] & 0x3F) | 0x80);

            string guid = new Guid(uuidBytes).ToString();
            return guid;
        }

        private bool IsValidUuid(string uuid)
        {
            return Guid.TryParse(uuid, out _);
        }

        public void ClearCachedToken()
        {
            _cachedAccessToken = null;
        }
    }
}
