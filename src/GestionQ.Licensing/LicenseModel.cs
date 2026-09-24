using System;
using System.Text.Json;
using System.Security.Cryptography;

namespace GestionQ.Licensing
{
    public class LicenseModel
    {
        public string HardwareId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public DateTime? ExpirationDate { get; set; }
        public DateTime IssuedDate { get; set; }
        
        public string Signature { get; set; } = string.Empty;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static LicenseModel? FromJson(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<LicenseModel>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
