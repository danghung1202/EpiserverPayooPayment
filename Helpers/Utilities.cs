using System.Security.Cryptography;
using System.Text;

namespace Foundation.Commerce.Payment.Payoo
{
    public class Utilities
    {
        public static string EncryptSHA512(string hashString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(hashString);
            using (var hash = SHA512.Create())
            {
                byte[] hashedInputBytes = hash.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
                StringBuilder hashedInputStringBuilder = new StringBuilder(128);
                foreach (byte b in hashedInputBytes)
                {
                    hashedInputStringBuilder.Append(b.ToString("X2"));
                }
                return hashedInputStringBuilder.ToString();
            }
        }
    }
}
