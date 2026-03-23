using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace LteCar.Server.Data
{
    public class User : EntityBase
    {
        public string? Name { get; set; }
        public int? ActiveVehicleId { get; set; }
        public Car? ActiveVehicle { get; set; }
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
        public long? TransferCode { get; set; }
        public long SessionId { get; set; }
        public DateTime? TransferCodeExpiresAt { get; set; }
        public ICollection<UserChannelDevice> UserChannelDevices { get; set; }
        public ICollection<UserCarSetup> CarSetups { get; set; }
        
        public bool HasControlledCar { get; set; }
        public string? PasswordHash { get; set; }
        public string? LoginName { get; set; }
        
        public void SetPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var hash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
            PasswordHash = $"{salt}:{hash}";
            LoginName = Name;
        }
        
        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(PasswordHash))
                return false;
            
            var parts = PasswordHash.Split(':');
            if (parts.Length != 2)
                return false;
            
            var salt = parts[0];
            var storedHash = parts[1];
            
            using var sha256 = SHA256.Create();
            var computedHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password + salt)));
            
            return storedHash == computedHash;
        }
    }
}