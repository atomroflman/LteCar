using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LteCar.Onboard;

public class SshKeyService
{
    private readonly ILogger<SshKeyService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;

    public SshKeyService(ILogger<SshKeyService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _privateKeyPath = "ssh_key";
        _publicKeyPath = "ssh_key.pub";
    }

    public string? GetPublicKey()
    {
        if (File.Exists(_publicKeyPath))
        {
            return File.ReadAllText(_publicKeyPath).Trim();
        }
        return null;
    }

    public bool VerifySignature(string data, string signature)
    {
        try
        {
            // Use C# RSA with properly imported PEM public key
            if (!File.Exists(_publicKeyPath))
            {
                _logger.LogError("Public key file not found");
                return false;
            }

            var publicKeyPem = File.ReadAllText(_publicKeyPath);
            
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);

            var isValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            if (isValid)
            {
                _logger.LogInformation("Signature verification succeeded");
            }
            else
            {
                _logger.LogWarning("Signature verification failed - signature does not match");
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying SSH signature");
            return false;
        }
    }


    public string? SignData(string data)
    {
        try
        {
            // Check if private key still exists (if not, it was downloaded and deleted)
            if (!File.Exists(_privateKeyPath))
            {
                _logger.LogError("Private key file not found - key may have been downloaded and deleted.");
                return null;
            }

            var privateKeyPem = File.ReadAllText(_privateKeyPath);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);

            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            return Convert.ToBase64String(signatureBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing data with SSH key");
            return null;
        }
    }

    public string? GenerateChallenge()
    {
        // Generate a random challenge for authentication
        return Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks.ToString("X");
    }

    public string? GenerateSessionIdFromPrivateKey(string privateKeyPem)
    {
        try
        {
            // Generate a deterministic session ID from the private key
            using var rsa = RSA.Create();
            rsa.ImportFromPem(privateKeyPem);
            
            // Get the public key bytes for consistent hashing
            var publicKeyBytes = rsa.ExportRSAPublicKey();
            
            // Create a hash of the public key for consistent session ID
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(publicKeyBytes);
            
            // Convert to a shorter, consistent string
            var sessionId = Convert.ToBase64String(hashBytes)[..16]; // First 16 characters
            
            return sessionId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating session ID from private key");
            return null;
        }
    }
}
