using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace GestionQ.Licensing
{
    public static class HardwareInfo
    {
        public static string GetHardwareId()
        {
            string motherboardId = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
            string cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");

            // Fallbacks in case WMI fails or returns empty (e.g. in some VMs)
            if (string.IsNullOrWhiteSpace(motherboardId) || motherboardId == "To be filled by O.E.M.")
                motherboardId = "UNKNOWN_MB";
            if (string.IsNullOrWhiteSpace(cpuId))
                cpuId = "UNKNOWN_CPU";

            string rawId = $"{motherboardId}-{cpuId}";
            
            // Hash it to make it look clean and standard
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
            
            // Return format: XXXX-XXXX-XXXX-XXXX
            string hex = BitConverter.ToString(bytes).Replace("-", "");
            return $"{hex.Substring(0, 4)}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
        }

        private static string GetWmiProperty(string wmiClass, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (var obj in searcher.Get())
                {
                    var val = obj[property]?.ToString();
                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        return val.Trim();
                    }
                }
            }
            catch
            {
                // Ignore exceptions (e.g., if WMI is restricted)
            }
            return string.Empty;
        }
    }
}
