using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Solhigson.Framework.Infrastructure;

namespace Solhigson.Framework.Utilities.Security
{
    public static class CryptoHelper
    {
        public static string GenerateJwtToken(string userIdentifier, string role, string email, string key, double expirationMinutes,
            string algorithm = SecurityAlgorithms.HmacSha512)
        {
            var claims = new List<Claim>()
            {
                new (ClaimTypes.NameIdentifier, userIdentifier),
                new (ClaimTypes.Email, email),
                new (ClaimTypes.Role, role)
            };
            return GenerateJwtToken(claims, key, expirationMinutes, algorithm);
        }

        public static string GenerateJwtToken(IEnumerable<Claim> claims, string key, double expirationMinutes,
            string algorithm = SecurityAlgorithms.HmacSha512)
        {
            var symmetricKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            var signingCredentials = new SigningCredentials(symmetricKey, algorithm);

            var securityTokenDescription = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                SigningCredentials = signingCredentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(securityTokenDescription);
            return tokenHandler.WriteToken(token);
        }

        private static RNGCryptoServiceProvider _rngProvider;
        private static RNGCryptoServiceProvider RngProvider
        {
            get { return _rngProvider ??= new RNGCryptoServiceProvider(); }
        }

        public static string HashData(string ordinaryData,
            HashAlgorithmType hashAlgorithmType = HashAlgorithmType.Sha512,
            StringEncodingType stringEncodingType = StringEncodingType.Hex)
        {
            return HashData(ordinaryData, Encoding.UTF8, hashAlgorithmType, stringEncodingType);
        }

        public static string HashData(string data,
            Encoding encodingToUse,
            HashAlgorithmType hashAlgorithmType = HashAlgorithmType.Sha512,
            StringEncodingType stringEncodingType = StringEncodingType.Hex)
        {
            HashAlgorithm hashAlgorithm = hashAlgorithmType switch
            {
                HashAlgorithmType.Md5 => new MD5CryptoServiceProvider(),
                HashAlgorithmType.Sha256 => new SHA256Managed(),
                HashAlgorithmType.Sha1 => new SHA1Managed(),
                _ => new SHA512Managed()
            };

            var tmpHash = hashAlgorithm.ComputeHash(encodingToUse.GetBytes(data));
            return stringEncodingType == StringEncodingType.Hex
                ? tmpHash.ToHexString()
                : Convert.ToBase64String(tmpHash);
        }
        
        public static string KeyHashData(string data, byte[] hashKey,
            Encoding encodingToUse,
            HashAlgorithmType hashAlgorithmType = HashAlgorithmType.Sha512,
            StringEncodingType stringEncodingType = StringEncodingType.Hex)
        {
            HashAlgorithm hashAlgorithm = hashAlgorithmType switch
            {
                HashAlgorithmType.Md5 => new HMACMD5(hashKey),
                HashAlgorithmType.Sha256 => new HMACSHA256(hashKey),
                HashAlgorithmType.Sha1 => new HMACSHA1(hashKey),
                _ => new HMACSHA512(hashKey)
            };

            var tmpHash = hashAlgorithm.ComputeHash(encodingToUse.GetBytes(data));
            return stringEncodingType == StringEncodingType.Hex
                ? tmpHash.ToHexString()
                : Convert.ToBase64String(tmpHash);
        }

        public static bool ValidateMacSha512Hex(string receivedMac, params object[] dataToMac)
        {
            return ValidateMac(receivedMac, HashAlgorithmType.Sha512, StringEncodingType.Hex, false, dataToMac);
        }
        
        public static bool ValidateMacSha512Base64(string receivedMac, params object[] dataToMac)
        {
            return ValidateMac(receivedMac, HashAlgorithmType.Sha512, StringEncodingType.Base64, false, dataToMac);
        }

        public static bool ValidateMac(string receivedMac,
            HashAlgorithmType hashAlgorithmType = HashAlgorithmType.Sha512,
            StringEncodingType stringEncodingType = StringEncodingType.Hex,
            bool throwExceptionIfValidationFails = true,
            params object[] dataToMac)
        {
            var concatenatedData = dataToMac.Aggregate("", (current, data) => current + data);

            var computedMac = HashData(concatenatedData, hashAlgorithmType, stringEncodingType);

            if (string.Compare(computedMac, receivedMac, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if(throwExceptionIfValidationFails)
                {
                    throw new Exception(StatusCode.MessageIntegrityValidationFailed + $"[{concatenatedData}]");
                }
                return false;
            }
            return true;
        }

        public static byte[] GenerateRandomBytes(int length)
        {
            var bytes = new byte[length];
            RngProvider.GetBytes(bytes);
            return bytes;
        }
        
        public static string GenerateRandomNumber(int digits)
        {
            var data = GenerateRandomBytes(digits);

            var randomNo = "";
            foreach (var t in data)
            {
                randomNo += Convert.ToInt32(t).ToString()[0].ToString();
            }
            return randomNo;
        }

        private const string AllowedChars = "abcdefghijklmnpqrstuvwxyzABCDEFGHIJKLMNPQRSTUVWXYZ123456789";
        public static string GenerateRandomString(int length,
            string allowedChars = AllowedChars)
        {
            if (length < 0)
            {
                return "";
            }
            if (string.IsNullOrEmpty(allowedChars))
            {
                allowedChars = AllowedChars;
            }

            const int byteSize = 256;
            var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
            if (byteSize < allowedCharSet.Length)
            {
                allowedCharSet = new HashSet<char>(allowedChars[..byteSize]).ToArray();
            }

            // Guid.NewGuid and System.Random are not particularly random. By using a
            // cryptographically-secure random number generator, the caller is always
            // protected, regardless of use.
            var result = new StringBuilder();
            var buf = new byte[128];
            while (result.Length < length)
            {
                RngProvider.GetBytes(buf);
                for (var i = 0; i < buf.Length && result.Length < length; ++i)
                {
                    // Divide the byte into allowedCharSet-sized groups. If the
                    // random value falls into the last group and the last group is
                    // too small to choose from the entire allowedCharSet, ignore
                    // the value in order to avoid biasing the result.
                    var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
                    if (outOfRangeStart <= buf[i]) continue;
                    result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
                }
            }
            return result.ToString();
        }

        public static ICryptoTransform CreateSymmetricEncryptor(byte[] encryptionKey, byte[] encryptionIv = null,
            EncryptionModes encryptionMode =
                EncryptionModes.TripleDes,
            PaddingMode paddingMode = PaddingMode.None,
            CipherMode cipherMode = CipherMode.CBC)
        {
            return CreateCryptographicInstance(encryptionKey, encryptionIv, encryptionMode, paddingMode, cipherMode);
        }

        public static ICryptoTransform CreateSymmetricDecryptor(byte[] encryptionKey, byte[] encryptionIv = null,
            EncryptionModes encryptionMode =
                EncryptionModes.TripleDes,
            PaddingMode paddingMode = PaddingMode.None,
            CipherMode cipherMode = CipherMode.CBC)
        {
            return CreateCryptographicInstance(encryptionKey, encryptionIv, encryptionMode, paddingMode, cipherMode,
                false);
        }
        
        public static async Task<byte[]> SymmetricEncryptAsync(byte[] dataToDecrypt, byte[] encryptionKey, byte[] encryptionIv = null,
            EncryptionModes encryptionMode =
                EncryptionModes.TripleDes,
            PaddingMode paddingMode = PaddingMode.None,
            CipherMode cipherMode = CipherMode.CBC)
        {
            return await PerformCryptoGraphicActionAsync(dataToDecrypt, CreateSymmetricEncryptor(encryptionKey, encryptionIv,
                encryptionMode, paddingMode, cipherMode));
        }
        
        public static async Task<byte[]> SymmetricDecryptAsync(byte[] dataToDecrypt, byte[] encryptionKey, byte[] encryptionIv = null,
            EncryptionModes encryptionMode =
                EncryptionModes.TripleDes,
            PaddingMode paddingMode = PaddingMode.None,
            CipherMode cipherMode = CipherMode.CBC)
        {
            return await PerformCryptoGraphicActionAsync(dataToDecrypt, CreateSymmetricDecryptor(encryptionKey, encryptionIv,
                encryptionMode, paddingMode, cipherMode));
        }

        private static ICryptoTransform CreateCryptographicInstance(byte[] encryptionKey, byte[] encryptionIv = null,
            EncryptionModes encryptionMode =
                EncryptionModes.TripleDes,
            PaddingMode paddingMode = PaddingMode.None,
            CipherMode cipherMode = CipherMode.CBC,
            bool createAsEncryptor = true)
        {
            SymmetricAlgorithm provider = encryptionMode switch
            {
                EncryptionModes.Aes => new AesCryptoServiceProvider {Mode = cipherMode, Padding = paddingMode},
                _ => new TripleDESCryptoServiceProvider {Mode = cipherMode, Padding = paddingMode}
            };
            return createAsEncryptor
                ? provider.CreateEncryptor(encryptionKey, encryptionIv)
                : provider.CreateDecryptor(encryptionKey, encryptionIv);
        }

        public static async Task<byte[]> PerformCryptoGraphicActionAsync(byte[] encodedText, ICryptoTransform cryptoProvider)
        {
            await using var ms = new MemoryStream();
            await using (var cs = new CryptoStream
                (ms, cryptoProvider, CryptoStreamMode.Write))
            {
                await cs.WriteAsync(encodedText, 0, encodedText.Length);
                cs.FlushFinalBlock();
            }

            return ms.ToArray();
        }
        
        public static async Task<byte[]> PerformCryptoGraphicActionAsync(string data, ICryptoTransform cryptoProvider)
        {
            return await PerformCryptoGraphicActionAsync(Encoding.UTF8.GetBytes(data), cryptoProvider);
        }

        
        private static string ToHexString(this byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }
            var output = new StringBuilder(bytes.Length);
            for (var i = 0; i < bytes.Length; i++)
            {
                output.Append(bytes[i].ToString("X2"));
            }
            return output.ToString();
        }

        private static string ToBase64String(this string data)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }
        
        private static string FromBase64String(this string data)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(data));
        }
    }
}