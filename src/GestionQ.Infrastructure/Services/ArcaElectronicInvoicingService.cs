using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace GestionQ.Infrastructure.Services
{
    public class ArcaElectronicInvoicingService : IElectronicInvoicingService
    {
        private readonly ILogger<ArcaElectronicInvoicingService> _logger;
        private readonly HttpClient _httpClient;
        
        // Homologation endpoints
        private const string WSAA_URL = "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";
        private const string WSFE_URL = "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
        
        private const string WSFE_SERVICE_NAME = "wsfe";
        
        private string _cuit = "20286759107"; // CUIT del titular del certificado

        private static string _cachedToken;
        private static string _cachedSign;
        private static DateTime _tokenExpiration = DateTime.MinValue;

        public ArcaElectronicInvoicingService(ILogger<ArcaElectronicInvoicingService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ElectronicInvoiceResponse> RequestCAEAsync(ElectronicInvoiceRequest request)
        {
            var response = new ElectronicInvoiceResponse();

            try
            {
                await EnsureAuthenticatedAsync();

                int nextVoucherNumber = await GetLastAuthorizedVoucherAsync(request.PointOfSaleNumber, request.InvoiceTypeCode) + 1;

                string soapEnvelope = BuildFECAESolicitarEnvelope(request, nextVoucherNumber, _cachedToken, _cachedSign, _cuit);

                var httpContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                httpContent.Headers.Add("SOAPAction", "\"http://ar.gov.afip.dif.FEV1/FECAESolicitar\"");

                var httpResponse = await _httpClient.PostAsync(WSFE_URL, httpContent);
                var responseXml = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"AFIP Error. Status: {httpResponse.StatusCode}. Content: {responseXml}");
                    response.Success = false;
                    response.Errors.Add("Error de conexión con ARCA. Código HTTP: " + httpResponse.StatusCode);
                    return response;
                }

                ParseFECAESolicitarResponse(responseXml, response, nextVoucherNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error request CAE");
                response.Success = false;
                response.Errors.Add($"Error interno de comunicación: {ex.Message}");
            }

            return response;
        }

        public async Task<int> GetLastAuthorizedVoucherAsync(int posNumber, int voucherTypeCode)
        {
            await EnsureAuthenticatedAsync();

            string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <FECompUltimoAutorizado xmlns=""http://ar.gov.afip.dif.FEV1/"">
      <Auth>
        <Token>{_cachedToken}</Token>
        <Sign>{_cachedSign}</Sign>
        <Cuit>{_cuit}</Cuit>
      </Auth>
      <PtoVta>{posNumber}</PtoVta>
      <CbteTipo>{voucherTypeCode}</CbteTipo>
    </FECompUltimoAutorizado>
  </soap:Body>
</soap:Envelope>";

            var httpContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
            httpContent.Headers.Add("SOAPAction", "\"http://ar.gov.afip.dif.FEV1/FECompUltimoAutorizado\"");

            var httpResponse = await _httpClient.PostAsync(WSFE_URL, httpContent);
            var responseXml = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"AFIP Error FECompUltimoAutorizado. HTTP {httpResponse.StatusCode}: {responseXml}");
            }

            var doc = XDocument.Parse(responseXml);
            XNamespace ns = "http://ar.gov.afip.dif.FEV1/";
            
            var result = doc.Descendants(ns + "FECompUltimoAutorizadoResult").FirstOrDefault();
            if (result != null)
            {
                var errors = result.Element(ns + "Errors");
                if (errors != null)
                {
                    var firstErr = errors.Elements(ns + "Err").FirstOrDefault();
                    if (firstErr != null)
                    {
                        var msg = firstErr.Element(ns + "Msg")?.Value;
                        throw new Exception($"AFIP Error: {msg}");
                    }
                }
                
                var cbteNro = result.Element(ns + "CbteNro")?.Value;
                if (int.TryParse(cbteNro, out int lastNro))
                {
                    return lastNro;
                }
            }

            return 0; // If no vouchers exist yet
        }

        public async Task<bool> CheckInfrastructureStatusAsync()
        {
            try
            {
                string soapEnvelope = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <FEDummy xmlns=""http://ar.gov.afip.dif.FEV1/"" />
  </soap:Body>
</soap:Envelope>";

                var httpContent = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                httpContent.Headers.Add("SOAPAction", "\"http://ar.gov.afip.dif.FEV1/FEDummy\"");

                var httpResponse = await _httpClient.PostAsync(WSFE_URL, httpContent);
                var responseXml = await httpResponse.Content.ReadAsStringAsync();

                var doc = XDocument.Parse(responseXml);
                XNamespace ns = "http://ar.gov.afip.dif.FEV1/";
                var dummyResult = doc.Descendants(ns + "FEDummyResult").FirstOrDefault();
                
                if (dummyResult != null)
                {
                    var appServer = dummyResult.Element(ns + "AppServer")?.Value;
                    var dbServer = dummyResult.Element(ns + "DbServer")?.Value;
                    var authServer = dummyResult.Element(ns + "AuthServer")?.Value;

                    return appServer == "OK" && dbServer == "OK" && authServer == "OK";
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task EnsureAuthenticatedAsync()
        {
            var certsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Certificados");
            var tokenCachePath = Path.Combine(certsFolder, "token_cache.xml");

            // 1. Check memory cache
            if (!string.IsNullOrEmpty(_cachedToken) && !string.IsNullOrEmpty(_cachedSign) && DateTime.Now < _tokenExpiration)
            {
                return; // Valid token
            }

            // 2. Check file cache
            if (File.Exists(tokenCachePath))
            {
                try
                {
                    var cacheDoc = XDocument.Load(tokenCachePath);
                    var expStr = cacheDoc.Root.Element("Expiration")?.Value;
                    if (DateTime.TryParse(expStr, out DateTime expDate) && DateTime.Now < expDate)
                    {
                        _cachedToken = cacheDoc.Root.Element("Token")?.Value;
                        _cachedSign = cacheDoc.Root.Element("Sign")?.Value;
                        _tokenExpiration = expDate;
                        return; // Valid token from file
                    }
                }
                catch { /* Ignore and request new token */ }
            }

            var keyPath = Path.Combine(certsFolder, "private.key");
            var crtPath = Path.Combine(certsFolder, "certificate.crt");

            if (!File.Exists(keyPath) || !File.Exists(crtPath))
            {
                throw new Exception("Faltan las credenciales criptográficas (private.key o certificate.crt) para autenticarse en ARCA.");
            }

            // 1. Create X509Certificate2 directly from PEM strings
            string keyPem = await File.ReadAllTextAsync(keyPath);
            string crtPem = await File.ReadAllTextAsync(crtPath);
            X509Certificate2 cert;
            try
            {
                cert = X509Certificate2.CreateFromPem(crtPem, keyPem);
                // Windows may require ephemeral key set for signing CMS
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    cert = new X509Certificate2(cert.Export(X509ContentType.Pkcs12, "temp"), "temp", X509KeyStorageFlags.EphemeralKeySet);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al leer el certificado PEM: {ex.Message}");
            }

            // 2. Generate LoginTicketRequest XML
            DateTime genTime = DateTime.UtcNow.AddMinutes(-5); // AFIP time tolerance
            DateTime expTime = DateTime.UtcNow.AddHours(12);

            string uniqueId = ((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()).ToString();
            string traXml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<loginTicketRequest version=""1.0"">
  <header>
    <uniqueId>{uniqueId}</uniqueId>
    <generationTime>{genTime:s}-00:00</generationTime>
    <expirationTime>{expTime:s}-00:00</expirationTime>
  </header>
  <service>{WSFE_SERVICE_NAME}</service>
</loginTicketRequest>";

            // 3. Sign TRA with CMS
            byte[] traBytes = Encoding.UTF8.GetBytes(traXml);
            var contentInfo = new ContentInfo(traBytes);
            var signedCms = new SignedCms(contentInfo);
            var cmsSigner = new CmsSigner(cert);
            
            signedCms.ComputeSignature(cmsSigner);
            byte[] cmsSignature = signedCms.Encode();
            string cmsBase64 = Convert.ToBase64String(cmsSignature);

            // 4. Send to WSAA
            string soapLogin = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:wsaa=""http://wsaa.view.sua.dgi.afip.gov.ar/"">
  <soapenv:Header/>
  <soapenv:Body>
    <wsaa:loginCms>
      <wsaa:in0>{cmsBase64}</wsaa:in0>
    </wsaa:loginCms>
  </soapenv:Body>
</soapenv:Envelope>";

            var httpContent = new StringContent(soapLogin, Encoding.UTF8, "text/xml");
            httpContent.Headers.Add("SOAPAction", "\"\"");
            
            var httpResponse = await _httpClient.PostAsync(WSAA_URL, httpContent);
            var responseXml = await httpResponse.Content.ReadAsStringAsync();

            if (!httpResponse.IsSuccessStatusCode)
            {
                throw new Exception($"Error de Autenticación AFIP (WSAA). HTTP {httpResponse.StatusCode}: {responseXml}");
            }

            // 5. Parse Token and Sign
            var doc = XDocument.Parse(responseXml);
            var loginCmsReturn = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "loginCmsReturn")?.Value;
            
            if (string.IsNullOrEmpty(loginCmsReturn))
                throw new Exception($"WSAA no retornó loginCmsReturn válido. XML: {responseXml}");

            var innerDoc = XDocument.Parse(loginCmsReturn);
            var token = innerDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "token")?.Value;
            var sign = innerDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "sign")?.Value;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(sign))
                throw new Exception("WSAA no retornó Token/Sign.");

            _cachedToken = token;
            _cachedSign = sign;
            _tokenExpiration = DateTime.Now.AddHours(11); // Safe buffer

            try
            {
                var docToSave = new XDocument(
                    new XElement("Ticket",
                        new XElement("Token", token),
                        new XElement("Sign", sign),
                        new XElement("Expiration", _tokenExpiration.ToString("o"))
                    )
                );
                docToSave.Save(tokenCachePath);
            }
            catch { /* Ignore if cannot save */ }
        }

        private string BuildFECAESolicitarEnvelope(ElectronicInvoiceRequest req, int voucherNumber, string token, string sign, string cuit)
        {
            string fechaCbte = DateTime.Now.ToString("yyyyMMdd");

            // Format numbers to 2 decimal places for AFIP
            string netStr = req.NetAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string vatStr = req.VatAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string exStr = req.ExemptAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            string totalStr = req.TotalAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

            string ivaXml = "";
            if (req.VatAmount > 0)
            {
                // Find Vat Base. Assuming 21% for simple example.
                ivaXml = $@"
              <Iva>
                <AlicIva>
                  <Id>5</Id> <!-- 21% -->
                  <BaseImp>{netStr}</BaseImp>
                  <Importe>{vatStr}</Importe>
                </AlicIva>
              </Iva>";
            }

            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
  <soap:Body>
    <FECAESolicitar xmlns=""http://ar.gov.afip.dif.FEV1/"">
      <Auth>
        <Token>{token}</Token>
        <Sign>{sign}</Sign>
        <Cuit>{cuit}</Cuit>
      </Auth>
      <FeCAEReq>
        <FeCabReq>
          <CantReg>1</CantReg>
          <PtoVta>{req.PointOfSaleNumber}</PtoVta>
          <CbteTipo>{req.InvoiceTypeCode}</CbteTipo>
        </FeCabReq>
        <FeDetReq>
          <FECAEDetRequest>
            <Concepto>{req.ConceptCode}</Concepto>
            <DocTipo>{req.DocTypeCode}</DocTipo>
            <DocNro>{(string.IsNullOrEmpty(req.DocNumber) ? "0" : req.DocNumber)}</DocNro>
            <CbteDesde>{voucherNumber}</CbteDesde>
            <CbteHasta>{voucherNumber}</CbteHasta>
            <CbteFch>{fechaCbte}</CbteFch>
            <ImpTotal>{totalStr}</ImpTotal>
            <ImpTotConc>0.00</ImpTotConc>
            <ImpNeto>{netStr}</ImpNeto>
            <ImpOpEx>{exStr}</ImpOpEx>
            <ImpTrib>0.00</ImpTrib>
            <ImpIVA>{vatStr}</ImpIVA>
            <MonId>PES</MonId>
            <MonCotiz>1.000</MonCotiz>
            <CondicionIVAReceptorId>{req.CondicionIVAReceptorId}</CondicionIVAReceptorId>
            {(req.CanMisMonExt ? "<Opcionales><Opcional><Id>CanMisMonExt</Id><Valor>1</Valor></Opcional></Opcionales>" : "")}
            {ivaXml}
          </FECAEDetRequest>
        </FeDetReq>
      </FeCAEReq>
    </FECAESolicitar>
  </soap:Body>
</soap:Envelope>";
        }

        private void ParseFECAESolicitarResponse(string responseXml, ElectronicInvoiceResponse response, int expectedVoucherNumber)
        {
            var doc = XDocument.Parse(responseXml);
            XNamespace ns = "http://ar.gov.afip.dif.FEV1/";
            
            var result = doc.Descendants(ns + "FECAESolicitarResult").FirstOrDefault();
            if (result == null)
            {
                response.Success = false;
                response.Errors.Add("Formato de respuesta desconocido.");
                return;
            }

            var errors = result.Element(ns + "Errors");
            if (errors != null)
            {
                foreach (var err in errors.Elements(ns + "Err"))
                {
                    string msg = err.Element(ns + "Msg")?.Value;
                    if (!string.IsNullOrEmpty(msg)) response.Errors.Add(msg);
                }
            }

            var feDetResp = result.Descendants(ns + "FECAEDetResponse").FirstOrDefault();
            if (feDetResp != null)
            {
                string rdo = feDetResp.Element(ns + "Resultado")?.Value; // A = Aprobado, R = Rechazado
                response.Status = rdo == "A" ? "Approved" : "Rejected";
                
                if (response.Status == "Approved")
                {
                    response.Success = true;
                    response.CAE = feDetResp.Element(ns + "CAE")?.Value;
                    
                    var vtoCae = feDetResp.Element(ns + "CAEFchVto")?.Value;
                    if (!string.IsNullOrEmpty(vtoCae) && vtoCae.Length == 8)
                    {
                        response.CAEExpirationDate = DateTime.ParseExact(vtoCae, "yyyyMMdd", null);
                    }
                    response.InvoiceNumber = expectedVoucherNumber;
                }

                var obs = feDetResp.Element(ns + "Observaciones");
                if (obs != null)
                {
                    foreach (var o in obs.Elements(ns + "Obs"))
                    {
                        string msg = o.Element(ns + "Msg")?.Value;
                        if (!string.IsNullOrEmpty(msg))
                        {
                            response.Warnings.Add(msg);
                            if (response.Status != "Approved")
                            {
                                response.Errors.Add(msg); // Treat obs as errors if rejected
                            }
                        }
                    }
                }
            }
            else
            {
                if (!response.Errors.Any())
                {
                    response.Success = false;
                    response.Errors.Add("La respuesta no contiene el detalle del comprobante.");
                }
            }
        }
    }
}
