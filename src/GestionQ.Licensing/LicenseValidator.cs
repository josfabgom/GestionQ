using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace GestionQ.Licensing
{
    public static class LicenseValidator
    {
        // Embedded public key for validation
        private const string PublicKeyPem = @"-----BEGIN RSA PUBLIC KEY-----
MIIBCgKCAQEAv26WfFntPNhxslFis8VkoDFO/m1SyAWOFy9CQdX+8rIyRXH+EVYG
Ea8s/zu0CKPWC0V4pqW6TLoSZ3v8DlIGnqBNOOTmYXfxO2ZZtxuQq9Oavz79HHwU
eJSRLC3tOlTmDJzKXLT8i2JV2+AHXUunW+MovA7aF+wBODKie62mETZpCTT+nF1x
KQzN8LZaH5DS6vfsPRbzb941rEJPAFO/oiplIJ9AWmjR54q1HIMBwf3OyvhYRv1x
+1CgT1KDgwhlFa2emNMKTNfMrzNcIpWV9U0rnKl+XM41HImhRGp9LHqvcfY37eJi
OMuBIBp1g5/0yU9bk+PRCQTbbc1RHnavIQIDAQAB
-----END RSA PUBLIC KEY-----";

        public static bool IsLicenseValid(string licenseBase64, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (string.IsNullOrWhiteSpace(licenseBase64))
            {
                errorMessage = "No se ha proporcionado una licencia.";
                return false;
            }

            try
            {
                byte[] jsonBytes = Convert.FromBase64String(licenseBase64);
                string json = Encoding.UTF8.GetString(jsonBytes);
                var license = LicenseModel.FromJson(json);
                
                if (license == null)
                {
                    errorMessage = "El formato de la licencia es invÃ¡lido.";
                    return false;
                }

                // 1. Verify Hardware ID
                string currentHardwareId = HardwareInfo.GetHardwareId();
                if (license.HardwareId != currentHardwareId)
                {
                    errorMessage = "La licencia no es vÃ¡lida para este servidor fÃ­sico (El Hardware ID no coincide).";
                    return false;
                }

                // 2. Verify Expiration
                if (license.ExpirationDate.HasValue && license.ExpirationDate.Value < DateTime.UtcNow)
                {
                    errorMessage = "La licencia ha expirado.";
                    return false;
                }

                // 3. Verify RSA Signature
                using var rsa = RSA.Create();
                rsa.ImportFromPem(PublicKeyPem);

                // Clone license and clear signature to reconstruct original data
                var dataToSign = new LicenseModel
                {
                    HardwareId = license.HardwareId,
                    ClientName = license.ClientName,
                    ExpirationDate = license.ExpirationDate,
                    IssuedDate = license.IssuedDate,
                    Signature = ""
                };

                byte[] dataBytes = Encoding.UTF8.GetBytes(dataToSign.ToJson());
                byte[] signatureBytes = Convert.FromBase64String(license.Signature);

                bool isSignatureValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                if (!isSignatureValid)
                {
                    errorMessage = "Firma digital invÃ¡lida. La licencia fue alterada o falsificada.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = "Error al leer la licencia: " + ex.Message;
                return false;
            }
        }
    }
}

