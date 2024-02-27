using System.Security.Cryptography;
using IdentityModel;
using System;
using System.Security.Cryptography.X509Certificates;
using Jose;
using System.Text;
using Newtonsoft.Json;
using Azure.Security.KeyVault.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;

namespace Perikato.Services
{
    public class OIDC_Service
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUrl { get; set; }

        //Azure KeyVault-palvelu
        public static (X509Certificate2, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret, KeyVaultSecret) ConnectToAzureKeyVault(IConfiguration configuration)
        {
            try
            {
                var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("AzureKeyVaultEndpoint") ?? throw new InvalidOperationException());
                var credential = new DefaultAzureCredential();

                // Luo asiakas salaisuuksien hakua varten
                var clientForSecrets = new SecretClient(keyVaultEndpoint, credential);

                // Hae OIDC-asiakkaan ID ja salaisuus
                KeyVaultSecret OIDC_Client_ID_Secret = clientForSecrets.GetSecret("OIDC-Client-ID");
                KeyVaultSecret OIDC_Client_Secret_Secret = clientForSecrets.GetSecret("ODIC-client-secret");

                //Perikato MS-SQL databasen salasana
                KeyVaultSecret PerikatoDBpass = clientForSecrets.GetSecret("PerikatoDBpass");

                //Perikato MongoDB databasen salasana
                KeyVaultSecret PerikatoMongoDBpass = clientForSecrets.GetSecret("PerikatoMongoDBpass");

                // Luo asiakas sertifikaattien hakua varten
                var ClientForCertificates = new CertificateClient(keyVaultEndpoint, credential);

                // Hae sertifikaatti (sisältäen politiikan ja muut metatiedot)
                KeyVaultCertificateWithPolicy certificate = ClientForCertificates.GetCertificate("CertificateInCertificates");

                // Hae sertifikaatin salaisuus, joka sisältää yksityisen avaimen
                KeyVaultSecret certificateSecret = clientForSecrets.GetSecret(certificate.Name);

                // Muunna salaisuuden arvo X509Certificate2-objektiksi
                // Annetaan oikea salasana sertifikaatin avaamiseen
                var certificatePassword = configuration["demo$app1$"]; // Salasana, jota käytettiin sertifikaatin suojaamiseen
                var privateKeyBytes = Convert.FromBase64String(certificateSecret.Value);
                var certificateWithPrivateKey = new X509Certificate2(
                    privateKeyBytes,
                    certificatePassword,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

                // Palautetaan sertifikaatti ja OIDC-tunnisteet
                return (certificateWithPrivateKey, OIDC_Client_ID_Secret, OIDC_Client_Secret_Secret, PerikatoDBpass, PerikatoMongoDBpass);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public (List<string>, string) DecryptToken(string encryptedJwt, X509Certificate2 certificate)
        {
            try
            {
                // // Varmista, että varmenteella on yksityinen avain
                if (!certificate.HasPrivateKey)
                {
                    throw new InvalidOperationException("The provided certificate does not have a private key.");
                }

                // Pura yksityinen RSA-avain varmenteesta
                var privateKey = certificate.GetRSAPrivateKey();

                // Pura JWE käyttämällä jose-jwt
                var decryptedToken = JWT.Decode(
                    encryptedJwt,
                    privateKey,
                    JweAlgorithm.RSA_OAEP_256,
                    JweEncryption.A128GCM
                );

                // Pilko/pura token palasiin
                string[] parts = decryptedToken.Split('.');
                if (parts.Length != 3)
                {
                    throw new InvalidOperationException("JWT does not have 3 parts!");
                }

                // Pura jokainen osa
                string header = Encoding.UTF8.GetString(Convert.FromBase64String(Base64UrlDecode(parts[0])));
                string payload = Encoding.UTF8.GetString(Convert.FromBase64String(Base64UrlDecode(parts[1])));

                // Pura/tallenna payload objektiin
                var payloadObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(payload);


                // Pura tiedot payloadista
                var claimsInfo = new List<string>();
                foreach (var claim in payloadObj)
                {
                    // // Pura avain ja arvo claimista
                    string key = claim.Key;
                    object value = claim.Value;

                    // Tallenna avainarvo-pari
                    claimsInfo.Add($"{key}: {value}");
                }

                return (claimsInfo, decryptedToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while decrypting the token: " + ex.Message);
                throw;
            }
        }

        //Purkaa dekoodatun tokenin palasiksi
        public static string Base64UrlDecode(string base64Url)
        {
            string padded = base64Url.Length % 4 == 0
                ? base64Url : base64Url + "====".Substring(base64Url.Length % 4);
            string base64 = padded.Replace("_", "/").Replace("-", "+");
            return base64;
        }
    }
}
